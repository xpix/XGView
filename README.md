XGView
======

Tool to display gcodes and support serial interface from GRBL

The Software state is BETA, please don't use it for ur productive Enviroment!!!

Here some points for control 3d view and serial window:

3D View:
- middle mouse, move camera
- right mouse, rotate camera
- middle scroll, zoom camera
- double middle mouse, go back to last view (Top, Front, ...)

Connect grbl:
- open Serial window: Menu->View->Serial Window
- set com port (will save for next call)
- set baudrate (will save for next call)
- Push connect and switch "Debug" on, you see the debug output with machine position every 0.2s

Control GRBL:
- Load gcode file: Menu->File->open
- Send gcode file: Play Button
- Pause Gcode send: Pause Button
- Cancel Gcode send: Cancel button
