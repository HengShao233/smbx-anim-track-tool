

#nullable enable
using System;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.Linq;
using Godot;

// ReSharper disable UnusedMember.Local
// ReSharper disable NotAccessedField.Local
// ReSharper disable MemberCanBePrivate.Local
// ReSharper disable MemberCanBePrivate.Global

namespace gd_project.addons.smbx_track_exporter;

public static class Encoder
{
    public const string CharSet = "0123456789ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz#$";
    public static readonly IReadOnlyDictionary<char, byte> CharToIndex =
        CharSet.Select((c, i) => (c, (byte)i)).ToDictionary();

    public static byte Encode(byte value)
    {
        if (value >= 64) return (byte)'0';
        return (byte)CharSet[value];
    }

    public static byte[] Encode<T>(T values) where T : IEnumerable<ushort>
    {
        var bytes = new List<byte>();
        foreach (var value in values)
        {
            var encoded = Encode(value);
            bytes.Add(encoded.a);
            bytes.Add(encoded.b);
            bytes.Add(encoded.c);
        }
        return bytes.ToArray();
    }

    public static (byte a, byte b, byte c) Encode(int value)
    {
        // 根据 char set 计算出 abc
        var a = (byte)CharSet[value % 64];
        var b = (byte)CharSet[value / 64 % 64];
        var c = (byte)CharSet[value / 64 / 64 % 64];
        return (c, b, a);
    }

    public static ushort Decode(in ReadOnlySpan<byte> bytes)
    {
        var value = 0ul;
        for (sbyte i = 0; i < bytes.Length; i++)
        {
            var byteValue = bytes[i];
            var index = (ulong)CharToIndex[(char)byteValue];
            value += index * AsciiBinary.AscBin.Pow(64, i);
        }
        return (ushort)value;
    }

    public static ushort ToUShort(double value)
        => (ushort)Math.Round(Math.Clamp(value, 0, ushort.MaxValue));
}


public partial class TrackExporterDock
{
    private struct TrackSettings
    {
        public int Idx = -1;
        public string? Template = "";

        public string? Multiplier
        {
            get => _multiplierSrc ?? "";
            set
            {
                _multiplierSrc = value?.Trim() ?? "";
                // 解析 multiplier
                if (string.IsNullOrWhiteSpace(_multiplierSrc))
                {
                    MultiplierNum = 1;
                    return;
                }

                var nums = _multiplierSrc.Split('/');
                if (nums.Length < 1) MultiplierNum = Parse(nums.Length > 0 ? nums[0] : "");
                else MultiplierNum = Parse(nums[0]) / Parse(nums[1]);
                return;

                static double Parse(string? s)
                {
                    s = s?.Trim().ToLower() ?? "";
                    if (string.IsNullOrEmpty(s)) return 1;

                    var inv = false;
                    if (s.StartsWith("inv_"))
                    {
                        inv = true;
                        s = s[3..];
                    }

                    if (!TryParseTail(s, "pi", Math.PI, inv, out var ret) ||
                        !TryParseTail(s, "e", Math.E, inv, out ret)) return ret;

                    if (!double.TryParse(s, out ret)) return 1;
                    if (inv) ret = 1 / ret;
                    return ret;

                    static bool TryParseTail(string s, string tail, double b, bool inv, out double ret)
                    {
                        ret = double.NaN;
                        if (!s.EndsWith(tail)) return false;

                        s = s[..^2];
                        var v = b;
                        if (!string.IsNullOrWhiteSpace(s))
                        {
                            if (!double.TryParse(s, out var piMul))
                            {
                                ret = 1;
                                return true;
                            }
                            v *= piMul;
                        }
                        if (inv) v = 1 / v;
                        ret = v;
                        return true;
                    }
                }
            }
        }

        public double MultiplierNum { get; private set; } = 1;

        private string? _multiplierSrc = "";

        public TrackSettings() {}

        public static TrackSettings FromDictionary(Godot.Collections.Dictionary? dict)
        {
            if (dict == null) return new TrackSettings();
            return new TrackSettings
            {
                Idx = GetInt(dict, "idx", -1),
                Template = GetStr(dict, "template", ""),
                Multiplier = GetStr(dict, "multiplier", "")
            };
        }

        public static Godot.Collections.Dictionary? ToDictionary(TrackSettings? settings = null)
        {
            if (settings == null) return null;
            return new Godot.Collections.Dictionary
            {
                { "idx", settings.Value.Idx },
                { "template", settings.Value.Template ?? "" },
                { "multiplier", settings.Value.Multiplier ?? "" },
            };
        }

        private static int GetInt(Godot.Collections.Dictionary dict, string key, int fallback)
        {
            return dict.TryGetValue(key, out var v) ? (int)v : fallback;
        }

        private static string GetStr(Godot.Collections.Dictionary dict, string key, string fallback)
        {
            return dict.TryGetValue(key, out var v) ? (string)v : fallback;
        }

        public struct ValueScale(short innerMultiplier = 1, short outerMultiplier = 1, short innerAddition = 0, short outerAddition = 0)
        {
            public const short MaxScale = short.MaxValue;
            public const short MinScale = short.MinValue;

            public short InnerMultiplier = innerMultiplier; // Mi
            public short OuterMultiplier = outerMultiplier; // Mo
            public short InnerAddition = innerAddition;     // Ai
            public short OuterAddition = outerAddition;     // Ao

            [Pure]
            public double GetValue(ushort value)
            {
                var v = (double)(ushort)(value & Value4d.ValueMask) - Value4d.IntegerOffset;
                var type = (ushort)(value & Value4d.TypeMask);

                // ReSharper disable once InvertIf
                if (type == Value4d.FixedTypeMask)
                {
                    const double denominator = Value4d.IntegerOffset;
                    v /= denominator;
                    v *= Value4d.DecimalScale;
                }

                return (
                           v *
                           InnerMultiplier +
                           InnerAddition
                       ) *
                       OuterMultiplier +
                       OuterAddition;
            }

            [Pure]
            public ushort GetNormalizedValue(double value)
            {
                const double denominator = Value4d.IntegerOffset;

                if (InnerMultiplier <= double.Epsilon) return 0;
                if (OuterMultiplier <= double.Epsilon) return 0;
                var ret = ((value - OuterAddition) / OuterMultiplier - InnerAddition) / InnerMultiplier;
                var absRet = Math.Abs(ret);
                var fixedValue = absRet / Value4d.DecimalScale * denominator;

                // ReSharper disable once InvertIf
                if (absRet is < Value4d.DecimalScale and > double.Epsilon && fixedValue > double.Epsilon)
                {
                    ret /= Value4d.DecimalScale;
                    var v = (ushort)((ToShort(ret * denominator) + Value4d.IntegerOffset) &
                                     Value4d.ValueMask);
                    return (ushort)(v | Value4d.FixedTypeMask);
                }
                return (ushort)(((ToShort(ret) + Value4d.IntegerOffset) & Value4d.ValueMask) | Value4d.IntTypeMask);
            }

            public static ValueScale GetValueScale(double maxValue, double minValue)
            {
                var ret = new ValueScale();

                // 确保 maxValue > minValue
                if (minValue > maxValue) (maxValue, minValue) = (minValue, maxValue);

                // 分配一组相对合理的缩放系数
                var range = Math.Abs(maxValue - minValue);
                // ReSharper disable once ConvertIfStatementToSwitchStatement
                if (range <= double.Epsilon)
                {
                    ret.OuterMultiplier = 0;
                    ret.InnerMultiplier = 0;
                    ret.InnerAddition = 0;
                    ret.OuterAddition = 0;

                    if (maxValue > MaxScale)
                    {
                        ret.OuterAddition = MaxScale;
                        var (a, b, sign) = SplitValue(maxValue - MaxScale);
                        ret.InnerAddition = ToShort(a);
                        ret.OuterMultiplier = ToShort(b * sign);
                    } else if (minValue < MinScale)
                    {
                        ret.OuterAddition = MinScale;
                        var (a, b, sign) = SplitValue(minValue - MinScale);
                        ret.InnerAddition = ToShort(a);
                        ret.OuterMultiplier = ToShort(b * sign);
                    }
                    else ret.OuterAddition = ToShort(maxValue);
                }
                else if (range <= Value4d.MaxInteger - Value4d.MinInteger)
                {
                    ret.OuterMultiplier = 1;
                    ret.InnerMultiplier = 1;

                    var mid = (maxValue + minValue) / 2;
                    ret.InnerAddition = ToShort(mid / 2);
                    ret.OuterAddition = ToShort(mid - ret.InnerAddition); // Ai+Ao 等于中数
                }
                else
                {
                    // 求解 kx+b=y -> (x=MinInteger,y=minValue)&(x=MaxInteger,y=maxValue)
                    var k = (maxValue - minValue) / (Value4d.MaxInteger - Value4d.MinInteger);
                    var b = maxValue - k * Value4d.MaxInteger;

                    // result = v * Mi*Mo + Ai*Mo+Ao
                    var (mi, mo, sign) = SplitValue(k, true);
                    if (Math.Abs(b) < double.Epsilon)
                    {
                        ret.OuterAddition = 0;
                        ret.InnerAddition = 0;
                        ret.InnerMultiplier = ToShort(mi);
                        ret.OuterMultiplier = ToShort(mo);
                    }
                    else
                    {
                        ret.InnerMultiplier = ToShort(mi * sign);
                        ret.OuterMultiplier = ToShort(mo);
                        mo = ret.OuterMultiplier; // 总是正值
                        // 把 b 值平摊到 Ai 和 Ao 上
                        // Ai*Mo+Ao = b, 令 Ai == Ao
                        // (Mo+1)*Ai = b
                        // Ai = b / (Mo+1)
                        var ai = b / (mo + 1);
                        ret.InnerAddition = ToShort(ai);
                        ret.OuterAddition = ToShort(b - ret.InnerAddition * mo);
                    }
                }
                return ret;
            }

            public static short ToShort(double value)
                => (short)Math.Clamp(Math.Round(value), MinScale, MaxScale);

            private static (double a, double b, double sign) SplitValue(double value, bool? isUpper = null)
            {
                if (value <= double.Epsilon) return (0, 0, 0);
                var sign = Math.Sign(value);

                value = Math.Abs(value);
                var sqr = Math.Sqrt(value);
                var sqrFloor = Math.Floor(sqr);
                var left = value / sqrFloor;
                return isUpper.HasValue
                    ? (sqrFloor, isUpper.Value ? Math.Ceiling(left) : Math.Floor(left), sign)
                    : (sqrFloor, Math.Round(left), sign);
            }
        }
    }

    private struct Value4d(double x, double y, double z, double w, byte dimension)
    {
        public const ushort DecimalScale = 20;
        public const ushort IntTypeMask = 0b0000_0000_0000_0000;
        public const ushort FixedTypeMask = 0b0100_0000_0000_0000;
        public const ushort ValueMask = 0b0011_1111_1111_1111;
        public const ushort TypeMask = 0b1100_0000_0000_0000;

        public const short IntegerOffset = 8191;
        public const short MinInteger = MinIntegerSrc - IntegerOffset;
        public const short MaxInteger = MaxIntegerSrc - IntegerOffset;

        public const ushort MinIntegerSrc = 0;
        public const ushort MaxIntegerSrc = 16383;

        public const byte MaxDimension = 4;

        public double X = x;
        public double Y = y;
        public double Z = z;
        public double W = w;
        public int KeyIdx = -1;
        public byte Dimension = dimension;

        public double this[int idx]
        {
            get => idx switch
            {
                0 => X,
                1 => Y,
                2 => Z,
                3 => W,
                _ => double.NaN,
            };
            set
            {
                _ = idx switch
                {
                    0 => X = value,
                    1 => Y = value,
                    2 => Z = value,
                    3 => W = value,
                    _ => double.NaN,
                };
            }
        }

        public static NormalizedValue4d Normalize(in Value4d value, in TrackSettings.ValueScale scale)
        {
            var ret = new NormalizedValue4d
            {
                Dimension = value.Dimension
            };
            for (var idx = 0; idx < value.Dimension; idx++)
            {
                var v = value[idx];
                var norV = scale.GetNormalizedValue(v);
                ret[idx] = norV;

                var finalV = scale.GetValue(norV);
                if (Math.Abs(finalV - v) <= 1) continue;

                GD.Print($"可能存在的误差: {finalV} != {v}, subtract: {finalV - v}");
                ret.OverflowMode =
                    finalV > v ? NormalizedValue4d.EOverflowMode.Max : NormalizedValue4d.EOverflowMode.Min;
            }
            return ret;
        }

        public static void UpdateExtremum(ref double max, ref double min, in Value4d value)
        {
            for (var idx = 0; idx < value.Dimension; idx++)
            {
                if (value[idx] > max) max = value[idx];
                if (value[idx] < min) min = value[idx];
            }
        }

        public static Value4d FromVariant(in Variant variant, in TrackSettings settings)
        {
            var template = settings.Template ?? "";
            var value = Parse(variant);
            template = template.Trim().ToLower();
            if (string.IsNullOrWhiteSpace(template)) return value;

            var ret = new Value4d();
            var idx = (byte)0;
            // ReSharper disable once ForeachCanBePartlyConvertedToQueryUsingAnotherGetEnumerator
            foreach (var c in template)
            {
                if (idx >= MaxDimension) break;
                if (!char.IsAsciiLetter(c)) continue;
                var newIdx = c switch
                {
                    'r' => 0,
                    'g' => 1,
                    'b' => 2,
                    'a' => 3,
                    'x' => 0,
                    'y' => 1,
                    'z' => 2,
                    'w' => 3,
                    _ => -1
                };
                if (newIdx < 0) continue;
                if (newIdx >= value.Dimension) newIdx = 0;

                ret[idx] = value[newIdx] * settings.MultiplierNum;
                idx++;
            }
            ret.Dimension = idx;
            return ret;
        }

        public static Value4d Parse(in Variant variant)
        {
            var ret = new Value4d();
            // ReSharper disable PossiblyImpureMethodCallOnReadonlyVariable
            // ReSharper disable once SwitchStatementMissingSomeEnumCasesNoDefault
            switch (variant.VariantType)
            {
                case Variant.Type.Bool:
                {
                    ret.X = variant.AsBool() ? 1 : 0;
                    ret.Dimension = 1;
                    break;
                }
                case Variant.Type.Int:
                {
                    ret.X = variant.AsInt64();
                    ret.Dimension = 1;
                    break;
                }
                case Variant.Type.Float:
                {
                    ret.X = variant.AsDouble();
                    ret.Dimension = 1;
                    break;
                }
                case Variant.Type.Vector2:
                {
                    var vec = variant.AsVector2();
                    ret.X = vec.X;
                    ret.Y = vec.Y;
                    ret.Dimension = 2;
                    break;
                }
                case Variant.Type.Vector2I:
                {
                    var vec = variant.AsVector2I();
                    ret.X = vec.X;
                    ret.Y = vec.Y;
                    ret.Dimension = 2;
                    break;
                }
                case Variant.Type.Rect2:
                {
                    var rect = variant.AsRect2();
                    ret.X = rect.Position.X;
                    ret.Y = rect.Position.Y;
                    ret.Z = rect.Size.X;
                    ret.W = rect.Size.Y;
                    ret.Dimension = 4;
                    break;
                }
                case Variant.Type.Rect2I:
                {
                    var rect = variant.AsRect2I();
                    ret.X = rect.Position.X;
                    ret.Y = rect.Position.Y;
                    ret.Z = rect.Size.X;
                    ret.W = rect.Size.Y;
                    ret.Dimension = 4;
                    break;
                }
                case Variant.Type.Vector3:
                {
                    var vec = variant.AsVector3();
                    ret.X = vec.X;
                    ret.Y = vec.Y;
                    ret.Z = vec.Z;
                    ret.Dimension = 3;
                    break;
                }
                case Variant.Type.Vector3I:
                {
                    var vec = variant.AsVector3I();
                    ret.X = vec.X;
                    ret.Y = vec.Y;
                    ret.Z = vec.Z;
                    ret.Dimension = 3;
                    break;
                }
                case Variant.Type.Vector4:
                {
                    var vec = variant.AsVector4();
                    ret.X = vec.X;
                    ret.Y = vec.Y;
                    ret.Z = vec.Z;
                    ret.W = vec.W;
                    ret.Dimension = 4;
                    break;
                }
                case Variant.Type.Vector4I:
                {
                    var vec = variant.AsVector4I();
                    ret.X = vec.X;
                    ret.Y = vec.Y;
                    ret.Z = vec.Z;
                    ret.W = vec.W;
                    ret.Dimension = 4;
                    break;
                }
                case Variant.Type.Plane:
                {
                    var plane = variant.AsPlane();
                    ret.X = plane.X;
                    ret.Y = plane.Y;
                    ret.Z = plane.Z;
                    ret.W = plane.D;
                    ret.Dimension = 4;
                    break;
                }
                case Variant.Type.Quaternion:
                {
                    var quat = variant.AsQuaternion();
                    ret.X = quat.X;
                    ret.Y = quat.Y;
                    ret.Z = quat.Z;
                    ret.W = quat.W;
                    ret.Dimension = 4;
                    break;
                }
                case Variant.Type.Color:
                {
                    var col = variant.AsColor();
                    ret.X = col.R;
                    ret.Y = col.G;
                    ret.Z = col.B;
                    ret.W = col.A;
                    ret.Dimension = 4;
                    break;
                }
            }
            // ReSharper restore PossiblyImpureMethodCallOnReadonlyVariable
            return ret;
        }

        public struct NormalizedValue4d(ushort x, ushort y, ushort z, ushort w, byte dimension)
        {
            [Flags]
            public enum EOverflowMode : ushort
            {
                None = 0,
                Min = 1 << 0,
                Max = 1 << 1,
            }

            public ushort X = x;
            public ushort Y = y;
            public ushort Z = z;
            public ushort W = w;
            public ushort Dimension = dimension;
            public EOverflowMode OverflowMode = EOverflowMode.None;

            public ushort this[int idx]
            {
                get => idx switch
                {
                    0 => X,
                    1 => Y,
                    2 => Z,
                    3 => W,
                    _ => 0
                };

                set
                {
                    _ = idx switch
                    {
                        0 => X = value,
                        1 => Y = value,
                        2 => Z = value,
                        3 => W = value,
                        _ => 0
                    };
                }
            }

            public void WriteTo(IList<ushort>? list)
            {
                if (list == null) return;
                for (var idx = 0; idx < Dimension; idx++) list.Add(this[idx]);
            }
        }
    }
}