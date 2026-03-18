' 动画轨道框架支持脚本

' 动画基础参数 begin
' head: [1char: anim_trackCount][2char*anim_trackCount: Track_StartPoint]
Dim Anim_Source As String = ""
Dim Anim_TrackCount As Integer = 0

Dim Track_Index As Long = -1
Dim Track_StartPoint As Long = 0
Dim Track_FrameLength As Integer = 0
Dim Track_FPS As Integer = 0
Dim Track_FrameCount As Integer = 0
Dim Track_InnerAddition As Integer = 0
Dim Track_OuterAddition As Integer = 0
Dim Track_InnerMultiplier As Integer = 1
Dim Track_OuterMultiplier As Integer = 1
Dim Track_Dimension As Integer = 0

Dim Frame_Type_L As Integer = 0
Dim Frame_Index_L As Integer = 0
Dim Frame_X_L As Double = 0
Dim Frame_Y_L As Double = 0
Dim Frame_Z_L As Double = 0
Dim Frame_W_L As Double = 0

Dim Frame_Type_R As Integer = 0
Dim Frame_Index_R As Integer = 0
Dim Frame_X_R As Double = 0
Dim Frame_Y_R As Double = 0
Dim Frame_Z_R As Double = 0
Dim Frame_W_R As Double = 0

Dim TweenFactor As Double = 0
' 动画基础参数 end

' 返回值 begin
Dim Frame_X_Ret As Double = 0
Dim Frame_Y_Ret As Double = 0
Dim Frame_Z_Ret As Double = 0
Dim Frame_W_Ret As Double = 0

' 获取返回值 x
Export Script AnimTrack_GetX(Return Double)
    Return Frame_X_Ret
End Script
' 获取返回值 y
Export Script AnimTrack_GetY(Return Double)
    Return Frame_Y_Ret
End Script
' 获取返回值 z
Export Script AnimTrack_GetZ(Return Double)
    Return Frame_Z_Ret
End Script
' 获取返回值 w
Export Script AnimTrack_GetW(Return Double)
    Return Frame_W_Ret
End Script
' 返回值 end

' 临时参数 begin
Dim AnimCommon_TempI0 As Long = 0
Dim AnimCommon_TempI1 As Long = 0
Dim AnimCommon_TempI2 As Long = 0
Dim AnimCommon_TempI3 As Long = 0
Dim AnimCommon_TempI4 As Long = 0
Dim AnimCommon_TempD0 As Double = 0
Dim AnimCommon_TempS0 As String = 0
' 临时参数 end

Script AnimTrackInner_Util_ClampFrame(frame As Integer, Return Integer)
    If Track_FrameCount <= 1 Then
        frame = 0
    ElseIf frame < 0 Then
        frame = Track_FrameCount - 1
    ElseIf frame >= Track_FrameCount Then
        frame = 0
    End If
    Return frame
End Script

Script AnimTrackInner_Util_FormatValue(value As Integer, Return Double)
    AnimCommon_TempI0 = CUMath_UInt16(value)
    If (AnimCommon_TempI0 And 49152) = 16384 Then
        AnimCommon_TempD0 = AnimCommon_TempI0 And 16383
        AnimCommon_TempD0 = AnimCommon_TempD0 / 8191
        AnimCommon_TempD0 = AnimCommon_TempD0 * 20
    Else
        AnimCommon_TempD0 = AnimCommon_TempI0 And 16383
    End If
    Return (AnimCommon_TempD0 * Track_InnerMultiplier + Track_InnerAddition) * Track_OuterMultiplier + Track_OuterAddition
End Script

Script AnimTrackInner_LoadTrack(track As Integer, Return Integer)
    If track < 0 Then
        track = Anim_TrackCount + track
    End If
    If track < 0 Then
        track = 0
    ElseIf track >= Anim_TrackCount And Anim_TrackCount > 0 Then
        track = Anim_TrackCount - 1
    End If

    If track = Track_Index Then
        Return -1
    End If
    Track_Index = track
    Track_FrameLength = 0
    Track_FPS = 0
    Track_FrameCount = 0
    Track_InnerAddition = 0
    Track_OuterAddition = 0
    Track_InnerMultiplier = 1
    Track_OuterMultiplier = 1
    Track_Dimension = 0
    Track_StartPoint = 0

    If Anim_TrackCount <= 0 Then
        Return -1
    End If
    AnimCommon_TempI0 = AscW(Mid(Anim_Source, track * 2 + 3, 1)) ' high byte
    AnimCommon_TempI1 = AscW(Mid(Anim_Source, track * 2 + 4, 1)) ' low byte
    Track_StartPoint = CUMath_AssembleInt16(AnimCommon_TempI0, AnimCommon_TempI1)

    If 0 = Track_StartPoint Then
        Return -1
    End If
    Track_StartPoint = Track_StartPoint + 1
    Track_FrameLength = AscW(Mid(Anim_Source, Track_StartPoint + 0, 1))
    Track_FPS = AscW(Mid(Anim_Source, Track_StartPoint + 1, 1))
    Track_FrameCount = AscW(Mid(Anim_Source, Track_StartPoint + 2, 1))
    Track_InnerAddition = AscW(Mid(Anim_Source, Track_StartPoint + 3, 1))
    Track_OuterAddition = AscW(Mid(Anim_Source, Track_StartPoint + 4, 1))
    Track_InnerMultiplier = AscW(Mid(Anim_Source, Track_StartPoint + 5, 1))
    Track_OuterMultiplier = AscW(Mid(Anim_Source, Track_StartPoint + 6, 1))
    Track_Dimension = AscW(Mid(Anim_Source, Track_StartPoint + 7, 1)) And 3
    Return -1
End Script

Script AnimTrackInner_SeekFrame(frame As Integer, Return Integer)
    If (Anim_TrackCount <= 0) Or (0 = Track_StartPoint) Then
        Return -1
    End If
    If frame < 1 Then
        frame = 1
    End If

    ' 二分查找帧区间
    If frame > Track_FrameLength Then
        frame = Track_FrameLength
    End If

    AnimCommon_TempI3 = 0
    If Track_FrameCount > 1 Then
        AnimCommon_TempI2 = 0 ' left frame idx
        AnimCommon_TempI3 = Track_FrameCount - 1 ' right frame idx
        AnimCommon_TempI4 = 0 ' curr frame idx
        ' 找到最后一个小于等于 frame 的帧
        Do While AnimCommon_TempI2 <= AnimCommon_TempI3
            AnimCommon_TempI4 = (AnimCommon_TempI2 + AnimCommon_TempI3) / 2 ' 中间帧 idx
            Call AnimTrackInner_LoadFrameHead_L(AnimCommon_TempI4)
            If frame = Frame_Index_L Then ' 精确命中
                AnimCommon_TempI3 = AnimCommon_TempI4
                Exit Do
            End If
            If frame > Frame_Index_L Then ' 大于 frame, 在右边
                AnimCommon_TempI2 = AnimCommon_TempI4 + 1
                Continue
            End If

            ' 小于 frame, 在左边
            AnimCommon_TempI3 = AnimCommon_TempI4 - 1
            Continue
        Loop
    End If

    ' AnimCommon_TempI3 为最后一个 <= frame 的帧 idx
    AnimCommon_TempI3 = AnimTrackInner_Util_ClampFrame(AnimCommon_TempI3)
    Call AnimTrackInner_LoadFrameHead_L(AnimCommon_TempI3)
    Call AnimTrackInner_LoadFrameValue_L(AnimCommon_TempI3)
    AnimCommon_TempI3 = AnimTrackInner_Util_ClampFrame(AnimCommon_TempI3 + 1)
    Call AnimTrackInner_LoadFrameHead_R(AnimCommon_TempI3)
    Call AnimTrackInner_LoadFrameValue_R(AnimCommon_TempI3)
    Return AnimCommon_TempI3
End Script

Script AnimTrackInner_LoadFrameHead_L(frame As Integer, Return Integer)
    frame = AnimTrackInner_Util_ClampFrame(frame)
    If frame >= Track_FrameCount Then
        Return 0
    End If
    Frame_Index_L = AscW(Mid(Anim_Source, Track_StartPoint + 8 + frame * (Track_Dimension + 1), 1))
    Frame_Type_L = (Frame_Index_L And 12288) >> 12
    Frame_Index_L = Frame_Index_L And 4095
    Return -1
End Script

Script AnimTrackInner_LoadFrameHead_R(frame As Integer, Return Integer)
    frame = AnimTrackInner_Util_ClampFrame(frame)
    If frame >= Track_FrameCount Then
        Return 0
    End If
    Frame_Index_R = AscW(Mid(Anim_Source, Track_StartPoint + 8 + frame * (Track_Dimension + 1), 1))
    Frame_Type_R = (Frame_Index_R And 12288) >> 12
    Frame_Index_R = Frame_Index_R And 4095
    Return -1
End Script

Script AnimTrackInner_LoadFrameValue_L(frame As Integer, Return Integer)
    frame = AnimTrackInner_Util_ClampFrame(frame)
    If frame >= Track_FrameCount Or Track_Dimension <= 0 Then
        Return 0
    End If
    Frame_Y_L = 0
    Frame_Z_L = 0
    Frame_W_L = 0
    Frame_X_L = AnimTrackInner_Util_FormatValue(AscW(Mid(Anim_Source, Track_StartPoint + 8 + frame * (Track_Dimension + 1) + 1, 1)))
    If Track_Dimension <= 1 Then
        Return -1
    End If
    Frame_Y_L = AnimTrackInner_Util_FormatValue(AscW(Mid(Anim_Source, Track_StartPoint + 8 + frame * (Track_Dimension + 1) + 2, 1)))
    If Track_Dimension <= 2 Then
        Return -1
    End If
    Frame_Z_L = AnimTrackInner_Util_FormatValue(AscW(Mid(Anim_Source, Track_StartPoint + 8 + frame * (Track_Dimension + 1) + 3, 1)))
    If Track_Dimension <= 3 Then
        Return -1
    End If
    Frame_W_L = AnimTrackInner_Util_FormatValue(AscW(Mid(Anim_Source, Track_StartPoint + 8 + frame * (Track_Dimension + 1) + 4, 1)))
    Return -1
End Script

Script AnimTrackInner_LoadFrameValue_R(frame As Integer, Return Integer)
    frame = AnimTrackInner_Util_ClampFrame(frame)
    If frame >= Track_FrameCount Or Track_Dimension <= 0 Then
        Return 0
    End If
    Frame_Y_R = 0
    Frame_Z_R = 0
    Frame_W_R = 0
    Frame_X_R = AnimTrackInner_Util_FormatValue(AscW(Mid(Anim_Source, Track_StartPoint + 8 + frame * (Track_Dimension + 1) + 1, 1)))
    If Track_Dimension <= 1 Then
        Return -1
    End If
    Frame_Y_R = AnimTrackInner_Util_FormatValue(AscW(Mid(Anim_Source, Track_StartPoint + 8 + frame * (Track_Dimension + 1) + 2, 1)))
    If Track_Dimension <= 2 Then
        Return -1
    End If
    Frame_Z_R = AnimTrackInner_Util_FormatValue(AscW(Mid(Anim_Source, Track_StartPoint + 8 + frame * (Track_Dimension + 1) + 3, 1)))
    If Track_Dimension <= 3 Then
        Return -1
    End If
    Frame_W_R = AnimTrackInner_Util_FormatValue(AscW(Mid(Anim_Source, Track_StartPoint + 8 + frame * (Track_Dimension + 1) + 4, 1)))
    Return -1
End Script

Export Script AnimTrack_Internal_Decode(s As String, Return String)
    AnimCommon_TempI0 = Len(s)
    If AnimCommon_TempI0 <= 4 Then
        Return ""
    End If

    AnimCommon_TempS0 = ""
    AnimCommon_TempI1 = 2
    Do While AnimCommon_TempI1 < AnimCommon_TempI0
        AnimCommon_TempS0 = AnimCommon_TempS0 & ChrW(CUMath_Int16(CUMath_Decode(s, AnimCommon_TempI1, 3, 64)))
        AnimCommon_TempI1 = AnimCommon_TempI1 + 3
    Loop
    Return ChrW(0) & AnimCommon_TempS0
End Script

' 装载动画轨道集
' @param s 动画轨道集
Export Script AnimTrack_Internal_PushSource(s As String)
    If s <> "" And AscW(Mid(s, 1, 1)) = 0 Then
        Anim_Source = s
        Anim_TrackCount = Asc(Mid(Anim_Source, 2, 1))
        If Anim_TrackCount < 0 Then
            Anim_TrackCount = 0
        End If
        Track_Index = -1
    Else
        Anim_Source = ""
        Anim_TrackCount = 0
    End If
End Script

' 根据动画帧计算当前轨道值, 并将结果推入返回值上下文, 返回成功与否
' @param track 轨道 idx
' @param frame 帧号
Export Script AnimTrack_CalcFrameValue(track As Integer, frame As Integer, Return Integer)
    Frame_X_Ret = 0
    Frame_Y_Ret = 0
    Frame_Z_Ret = 0
    Frame_W_Ret = 0

    Call AnimTrackInner_LoadTrack(track)
    AnimCommon_TempI3 = AnimTrackInner_SeekFrame(frame)
    If AnimCommon_TempI3 < 0 Then
        Return 0
    ElseIf AnimCommon_TempI3 <= 0 Then
        AnimCommon_TempI3 = Track_FrameLength - Frame_Index_L
    Else
        AnimCommon_TempI3 = Frame_Index_R - Frame_Index_L
    End If
    frame = frame - Frame_Index_L

    If AnimCommon_TempI3 <= 0.0001 Then
        Frame_X_Ret = Frame_X_L
        Frame_Y_Ret = Frame_Y_L
        Frame_Z_Ret = Frame_Z_L
        Frame_W_Ret = Frame_W_L
        Return -1
    End If

    AnimCommon_TempD0 = (frame - Frame_Index_L) / AnimCommon_TempD0
    Frame_X_Ret = CUMath_Lerp(Frame_X_L, Frame_X_R, AnimCommon_TempD0)
    Frame_Y_Ret = CUMath_Lerp(Frame_Y_L, Frame_Y_R, AnimCommon_TempD0)
    Frame_Z_Ret = CUMath_Lerp(Frame_Z_L, Frame_Z_R, AnimCommon_TempD0)
    Frame_W_Ret = CUMath_Lerp(Frame_W_L, Frame_W_R, AnimCommon_TempD0)
    Return -1
End Script

' 根据归一化参数计算当前轨道值, 并将结果推入返回值上下文, 返回成功与否
' @param track 轨道 idx
' @param t 归一化参数, 范围 [0, 1]
Export Script AnimTrack_CalcValue(track As Integer, t As Double, Return Integer)
    Call AnimTrackInner_LoadTrack(track)
    Return AnimTrack_CalcFrameValue(track, CUMath_Frac(t) * Track_FrameLength)
End Script
