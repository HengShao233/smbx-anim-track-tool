'Script No.4 Name:New Script

call exescript(Libs)
call exescript(AnimTrack)
call exescript(Anim)

Call AnimTrackLoad_Walk()
call showmsg(cstr(animtrack_getx()) & ", " & cstr(animtrack_gety()))

' pos dim
Dim Pos_x As Integer = 400
Dim Pos_y As Integer = 300
Dim PointBmpId_start As Integer = 1
Dim LineBmpId_start As Integer = 14

' create bmp
Call BmpNewStoreIsUseScreenCoords(1)
Call BmpNewStorePos(0, 0)
Call BmpNewStoreScale(0.1, 0.1)
Call BmpNewStoreSrc(1, 0, 0, 64, 64)
Dim i As Integer = 0

' create point
For i = 0 To 12 Step 1
    Call BmpNew(PointBmpId_start + i)
Next

' create line
Call BmpNewStoreIsUseScreenCoords(1)
Call BmpNewStorePos(0, 0)
Call BmpNewStoreScale(2, 2)
Call BmpNewStoreSrc(1, 0, 0, 1, 1)
For i = 0 To 7 Step 1
    Call BmpNew(LineBmpId_start + i)
Next

Dim timestamp As Long = 0
Dim t As Double = 0

Dim temp_x_to As Integer = 0
Dim temp_y_to As Integer = 0
Dim temp_x_from As Integer = 0
Dim temp_y_from As Integer = 0

Dim temp_pointId_from As Integer = 0
Dim temp_pointId_to As Integer = 0

Do
  t = timestamp / 60

  ' update points
  For i = 1 To 12 Step 1
    call AnimTrack_CalcValue(i, t)
    call BmpStoreAnchor(0.5, 0.5)
    call BmpPos(PointBmpId_start + i, Pos_x + animtrack_getx(), Pos_y + animtrack_gety())
  Next

  ' update lines
  For i = 0 To 7 Step 1
    temp_pointId_from = 0
    temp_pointId_to = 0
    Select Case i
      Case 0
        temp_pointId_from = 9
        temp_pointId_to = 1
      Case 1
        temp_pointId_from = 10
        temp_pointId_to = 2
      Case 2
        temp_pointId_from = 1
        temp_pointId_to = 3
      Case 3
        temp_pointId_from = 2
        temp_pointId_to = 4
      Case 4
        temp_pointId_from = 11
        temp_pointId_to = 5
      Case 5
        temp_pointId_from = 12
        temp_pointId_to = 6
      Case 6
        temp_pointId_from = 5
        temp_pointId_to = 7
      Case 7
        temp_pointId_from = 6
        temp_pointId_to = 8
    End Select
    If temp_pointId_from <= 0 Then
      Continue
    End If

    ' get pos
    Call BmpStoreAnchor(0.5, 0.5)
    call BmpGetPos(temp_pointId_from + PointBmpId_start)
    temp_x_from = BmpGetRetX()
    temp_y_from = BmpGetRetY()
    call BmpGetPos(temp_pointId_to + PointBmpId_start)
    temp_x_to = BmpGetRetX()
    temp_y_to = BmpGetRetY()

    temp_x_to = temp_x_to - temp_x_from
    temp_y_to = temp_y_to - temp_y_from

    ' draw line
    Call BmpStoreAnchor(0, 0)
    call BmpPos(LineBmpId_start + i, temp_x_from, temp_y_from)
    call BmpScale(LineBmpId_start + i, CUMath_VecLength(temp_x_to, temp_y_to), 1)
    call BmpRotate(LineBmpId_start + i, CUMath_Angle(1, 0, temp_x_to, temp_y_to))
  Next

	call sleep(1)
  timestamp = timestamp + 1
Loop
