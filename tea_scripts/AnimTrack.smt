' 动画轨道框架支持脚本

' 解码逻辑
Dim libCurve_tempH As Long = 0             ' 临时变量 H
Dim libCurve_tempI As Long = 0             ' 临时变量 I
Dim libCurve_tempJ As Long = 0             ' 临时变量 J

' 向量运算结果返回值
Dim libCurve_retX As Double = 0            ' 返回值 X
Dim libCurve_retY As Double = 0            ' 返回值 Y

' 简单幂运算
' @param x 基数
' @param pow 指数 (> 0)
Export Script CUMath_SimplePow(x As Long, pow As Integer, Return Long)
    If pow <= 0 Then
        Return 1
    End If
    Do While pow <> 0
        If pow And 1 Then
            libCurve_tempI = libCurve_tempI * x
        End If
        x = x * x
        pow = pow >> 1
    Loop
    Return libCurve_tempI
End Script

' 字符转码 (92 进制)
' @param c 字符 " !#$%&'()*+,-./0123456789:;<=>?@ABCDEFGHIJKLMNOPQRSTUVWXYZ[]^_abcdefghijklmnopqrstuvwxyz{|}~"
Export Script CUMath_Char92Code(c As Integer, Return Integer)
    If c < 32 Or c > 126 Then
        Return -1 ' invalid
    End If

    If c > 96 Then ' 扣除反引号
        c -= 1
    End If
    If c > 92 Then ' 扣除反斜杠
        c -= 1
    End If
    If c > 34 Then ' 扣除引号
        c -= 1
    End If
    Return c - 32
End Script

' 字符转码 (64 进制)
' @param c 字符 "0123456789ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz#$"
Export Script CUMath_Char64Code(c As Integer, Return Integer)
    If c >= 48 And c <= 57 Then
        Return c - 48 ' 数字, 0-9
    ElseIf c >= 65 And c <= 90 Then
        Return c - 65 + 10 ' 大写字母, A-Z: 10-35
    ElseIf c >= 97 And c <= 122 Then
        Return c - 97 + 36 ' 小写字母, a-z: 36-61
    ElseIf 35 = c Then
        Return 62 ' #
    ElseIf 36 = c Then
        Return 63 ' $
    End If
    Return -1 ' invalid
End Script

' 字符转码 (36 进制, 兼容 16 进制)
' @param c 字符 "0-9A-Z" 或 "0-9a-z"
Export Script CUMath_Char36Code(c As Integer, Return Integer)
    If c >= 48 And c <= 57 Then
        Return c - 48 ' 数字, 0-9
    ElseIf c >= 65 And c <= 90 Then
        Return c - 65 + 10 ' 大写字母, A-Z: 10-35
    ElseIf c >= 97 And c <= 122 Then
        Return c - 97 + 10 ' 小写字母, a-z: 10-35
    End If
    Return -1 ' invalid
End Script

' 字符串转码 (2-92进制)
' @param s 字符串
' @param start 开始位置
' @param lenght 长度
' @param base 进制
Export Script CUMath_Decode(s As Integer, start As Integer, lenght As Integer, base As Integer, Return Long)
    If start + lenght - 1 > Len(s) Then
        lenght = Len(s) - start + 1
    End If
    If lenght <= 0 Or start <= 0 Then
        Return -1
    End If
    If base < 2 Or base > 92 Then
        Return -1
    End If

    libCurve_tempJ = 0
    If base <= 36 Then
        For libCurve_tempH = 0 To lenght Step 1
            libCurve_tempI = CUMath_Char36Code(Asc(Mid(s, start + libCurve_tempH, 1))) * CUMath_SimplePow(base, libCurve_tempH)
            If libCurve_tempI < 0 Then
                Return -1
            End If
            libCurve_tempJ = libCurve_tempJ + libCurve_tempI
        Next
    ElseIf base <= 64 Then
        For libCurve_tempH = 0 To lenght Step 1
            libCurve_tempI = CUMath_Char64Code(Asc(Mid(s, start + libCurve_tempH, 1))) * CUMath_SimplePow(base, libCurve_tempH)
            If libCurve_tempI < 0 Then
                Return -1
            End If
            libCurve_tempJ = libCurve_tempJ + libCurve_tempI
        Next
    ElseIf base <= 92 Then
        For libCurve_tempH = 0 To lenght Step 1
            libCurve_tempI = CUMath_Char92Code(Asc(Mid(s, start + libCurve_tempH, 1))) * CUMath_SimplePow(base, libCurve_tempH)
            If libCurve_tempI < 0 Then
                Return -1
            End If
            libCurve_tempJ = libCurve_tempJ + libCurve_tempI
        Next
    End If
    Return libCurve_tempJ
End Script

' 拼接两个整数
' @param a 整数
' @param b 整数
' @return long 整数
Export Script CUMath_AssembleInt(a As Integer, b As Integer, Return Long)
    libCurve_tempH = a And 65535
    libCurve_tempJ = b And 65535
    Return libCurve_tempJ or (libCurve_tempH << 16)
End Script

' 拆解整数 
' @param value 整数
' @return 通过 CUMath_GetVecRetX 和 CUMath_GetVecRetY 获取结果
Export Script CUMath_DisassembleInt(value As Long)
    libCurve_tempH = (value >> 16) and 65535
    libCurve_tempJ = value And 65535
    libCurve_retX = libCurve_tempH
    libCurve_retY = libCurve_tempJ
End Script

' 动画基础参数 begin
' head: [1char: anim_trackCount][2char*anim_trackCount: Track_StartPoint]
Dim Anim_Source As String = ""
Dim Anim_TrackCount As Integer = -1

Dim Track_Index As Long = -1
Dim Track_StartPoint As Long = 0
Dim Track_FrameLenght As Integer = 0
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

' 临时参数 begin
Dim Anim_TempI0 As Long = 0
Dim Anim_TempI1 As Long = 0
' 临时参数 end

Script AnimTrackInner_LoadTrack(track As Integer, Return Integer)
    If track < 0 Then
        Return 0
    End If
    If track >= Anim_TrackCount Then
        Return 0
    End If
    Track_Index = track
    Track_FrameLenght = 0
    Track_FPS = 0
    Track_FrameCount = 0
    Track_InnerAddition = 0
    Track_OuterAddition = 0
    Track_InnerMultiplier = 1
    Track_OuterMultiplier = 1
    Track_Dimension = 0

    Anim_TempI0 = AscW(Mid(Anim_Source, track * 2 + 2, 1)) ' high byte
    Anim_TempI1 = AscW(Mid(Anim_Source, track * 2 + 3, 1)) ' low byte
    Track_StartPoint = CUMath_AssembleInt(Anim_TempI0, Anim_TempI1)

    If 0 = Track_StartPoint Then
        Return -1
    End If
    Track_FrameLenght = AscW(Mid(Anim_Source, Track_StartPoint + 0, 1))
    Track_FPS = AscW(Mid(Anim_Source, Track_StartPoint + 1, 1))
    Track_FrameCount = AscW(Mid(Anim_Source, Track_StartPoint + 2, 1))
    Track_InnerAddition = AscW(Mid(Anim_Source, Track_StartPoint + 3, 1))
    Track_OuterAddition = AscW(Mid(Anim_Source, Track_StartPoint + 4, 1))
    Track_InnerMultiplier = AscW(Mid(Anim_Source, Track_StartPoint + 5, 1))
    Track_OuterMultiplier = AscW(Mid(Anim_Source, Track_StartPoint + 6, 1))
    Track_Dimension = AscW(Mid(Anim_Source, Track_StartPoint + 7, 1)) And 3
    Return -1
End Script

Script AnimTrackInner_LoadFrame(frame As Integer, Return Integer)
    ' TODO: 读取帧头
End Script

Export Script AnimTrack_Internal_PushSource(s As String)
    Anim_Source = s
    Anim_TrackCount = CUMath_Decode(Anim_Source, 0, 1, 64)
End Script

Export Script AnimTrack_SeekFrame(track As Integer, frame As Integer, Return Integer)
    If track < 0 Then
        Return 0
    End If
    If track >= Anim_TrackCount Then
        Return 0
    End If
    If frame < 1 Then
        frame = 1
    End If

    If Track_Index <> track Then
        If Not AnimTrackInner_LoadTrack(track) Then
            Return 0
        End If
        If 0 = Track_StartPoint Then
            Return 1
        End If
    End If

    ' 二分查找帧
    If frame > Track_FrameLenght Then
        frame = Track_FrameLenght
    End If

    Return frame
End Script
