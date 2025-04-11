# Pegasus Flat

NINA plugin  to control a Pegasus Powerbox PWM port like a flat panel - for example using the flat wizard.

When this plugin is active, it will connect to the Pegasus Unity service running on localhost, auto discover any ports it can control and list each one of them as a flat device. It references them by the name configured in Unity.


Note: by default the pegasus PWM ports have a duty cycle of just 3khz - this can be changed to go up to 20khz using "hidden" features which can help if you notice any flickering or inconsistencies because of the low frequency.
