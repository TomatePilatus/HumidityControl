# HumidityControl

A simple project for controlling the humidity of a plexiglass box.
The controller is a Netduino board that reads the temperature, humidity, and pressure data from a BME680 to display it on a LCD screen.
Through a 5-Position Switch-Position Switch—from Parallax,—the user sets the speed of a blower fan that moves air against some silica gel.

## What you can find here

A working project targeting the .NET Micro Framework v4.2 for Netduino, along with:

- A BME680 driver with no support for the heating functionality.
- A Sparkfun Graphic LCD Serial Backpack driver.

## References:

- [Fan Control](https://www.arduined.eu/arduino-pwm-pc-fan-control/)
- [LM317 adjustable voltage regulator](https://microcontrollerslab.com/lm317-adjustable-voltage-regulator/)

### Credits

The BME680 driver is based on [BME680 Sensor](https://github.com/georgemathieson/bme680) of [George Mathieson](https://github.com/georgemathieson).