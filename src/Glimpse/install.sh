#!/bin/bash

installationDirectory="$HOME/.local/bin/"

echo
echo "### Creating ${installationDirectory}"
mkdir $installationDirectory

echo
echo "### Copy binary to ${installationDirectory}"
cp ./Glimpse $installationDirectory

echo
echo "### Updating SUPER_L shortcut"
xfconf-query \
	-c xfce4-keyboard-shortcuts \
	-p "/commands/custom/<Primary>Escape" \
	-s "gdbus call --session --dest org.glimpse --object-path /org/glimpse --method org.gtk.Actions.Activate \"OpenStartMenu\" [] {}"

echo
echo "### Disabling XFCE panel and adding Glimpse to the failsafe session (the default X session)"

xfceStartupNumApps=$(xfconf-query -c xfce4-session -p /sessions/Failsafe/Count)

for ((i=0 ; i<$xfceStartupNumApps; i++)); do
	xfceApp=$(xfconf-query -c xfce4-session -p "/sessions/Failsafe/Client${i}_Command" | awk 'FNR == 3 {print}')
	if [[ $xfceApp == "xfce4-panel" ]]
	then
		xfconf-query -c xfce4-session -p "/sessions/Failsafe/Client${i}_Command" -s "${installationDirectory}Glimpse" -a
	fi
done

echo
echo "### Stopping XFCE panel"
pkill -9 xfce4-panel

echo
echo "### Removing ayatana-indicator-application"
sudo apt-get purge ayatana-indicator-application
pkill -9 ayatana

echo
echo "Installation complete"
echo "*** If you use a saved X session then you will need to save a new one with Glimpse running"
