#!/bin/bash

installationDirectory="$HOME/.local/bin/"

echo
echo "### Remove binary from ${installationDirectory}"
rm ${installationDirectory}/glimpse

echo
echo "### Reset SUPER_L shortcut"
xfconf-query -c xfce4-keyboard-shortcuts -p "/commands/custom/<Primary>Escape" -r

echo
echo "### Enabling XFCE panel for failsafe session"

xfceStartupNumApps=$(xfconf-query -c xfce4-session -p /sessions/Failsafe/Count)

for ((i=0 ; i<$xfceStartupNumApps; i++)); do
	xfceApp=$(xfconf-query -c xfce4-session -p "/sessions/Failsafe/Client${i}_Command" | awk 'FNR == 3 {print}')
	if [[ $xfceApp =~ /glimpse$ ]]
	then
		xfconf-query -c xfce4-session -p "/sessions/Failsafe/Client${i}_Command" -r
	fi
done

echo "### Stopping Glimpse"
pkill -9 glimpse

echo
echo "### Installing ayatana-indicator-application"
sudo apt-get install ayatana-indicator-application

echo
echo "Uninstall complete"
echo "*** If you have a saved X session then you will need to start xfce4-panel and save your session"
