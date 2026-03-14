' 动画轨道框架支持脚本

' 动画基础参数 begin
' head: [1char: anim_trackCount][2char*anim_trackCount: Track_StartPoint]
Dim Anim_Source As String = ""
Dim Anim_TrackCount As Integer = -1

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
    Track_FrameLength = 0
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
    If frame > Track_FrameLength Then
        frame = Track_FrameLength
    End If

    Return frame
End Script
