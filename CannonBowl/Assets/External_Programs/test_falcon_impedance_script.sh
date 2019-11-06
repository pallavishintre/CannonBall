#!/bin/bash

#Warning just in case
read -n 1 -s -r -p "Falcon will apply force to center itself after pressing a key. Use ctrl-c to end"

#Have to use this path because this will be called from the top-level directory of the Unity project
./Assets/External_Programs/test_falcon_impedance
