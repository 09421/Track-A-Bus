   M S B A M L   `   `   `  ����   GPSsimu    TrackABusSim.MainWindow   \ XPresentationFramework, Version=4.0.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35R NWindowsBase, Version=4.0.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35W SPresentationCore, Version=4.0.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35D 9http://schemas.microsoft.com/winfx/2006/xaml/presentation    R NSystem.Xaml, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089:x,http://schemas.microsoft.com/winfx/2006/xaml     -     � Title$  TrackABus Simulator��$	��600��$	��800�� � 	Resources ��%    .���  � RowDefinitions �� $��1* ��� $��1* � � ColumnDefinitions �� $��1* ��� $��1* �.+��� -   $��	BusConfig��
���   �$��1q�$��Bus Configurations��.���  �� $��1* ��� $
��3.5* ��� $��1* ��� $
��3.5* ��� $��1* ��� $
��3.5* ��� $��1* ��� $
��3.5* ��� $
��1.0* ��� $
��3.5* ��� $��1* ��� $
��3.5* ��� $��1* � �� $��2* ��� $��3* �.+��� $��BusNr:��$��1��$��Center=�$��Center=�$
��Bold"��� $��Update speed (s):  ��$��3��$��Center=�$��Center=�$
��Bold"��� $��Simulation mode ��$��5��$��Center=�$��Center=�$
��Bold"��� $��Bus direction��$��7��$��Center=�$��Center=�$
��Bold"��� -   $��MinSpeedBlock��$��Minimum speed: 30��$��9��$��Center=�$��Center=�$
��Bold"��� -   $��MaxSpeedBlock��$��Maximum speed: 50��$��11��$��Center=�$��Center=�$
��Bold"��� -   $��
BusNrCombo��$��1��$��1��$��Center=�$��5,0,0,0q��� -   $��UpdateSpeedBox��$��1��$��3��$��Center=�$��5,0,0,0q��� -   $��SimuModeCombo��$��1��$��5��$��Center=�$��5,0,0,0q��� -   $��BusDirectionCombo��$��1��$��7��$��Center=�$��5,0,0,0q�{�.  �� -	   $��MinSpeedSLider��$��1��$��9�� � Minimum$ 30Q� � Maximum$
 1000Q� � Value$ 30Q�$��Center=�$��Stretch=��� -
   $��MaxSpeedSLider��$��1��$��11��$ 50Q�$
 1000Q�$ 30Q�$��Center=�$��Stretch=��� $��Controls��$��1��
���   �$��1q�.���  �� $��10 ��� $��1* ��� $��10 ��� $��1* ��� $��10 � �� $��10 ��� $��1* ��� $��10 ��� $��1* ��� $��10 �.+��� -   $��StartStopButton��$��20(�$
��Bold"�$��1��$��1��
��� � �.���� -   $��ContentBlock1�� �� TextAlignment$ Center=�Start randomly�� 
on route�� -   $��ClearLogButton��$��20(�$
��Bold"�$��3��$��3��$��	Clear Log���� -   $��RefreshButton��$��20(�$
��Bold"�$��3��$��1��$��Refresh���� -    ��Name$ StartStopFirstPointButton��$��20(�$
��Bold"�$��1��$��3��
��� � �.���� -   $��ContentBlock2��$ Center=�
Start at�� 	beginning�� 
of route�� $	��Log��$��1��$��2��
���   �$��1q�.���� -   $��LogText��	 �� 
IsReadOnly	 . "
 �� VerticalScrollBarVisibility$

 Auto=�