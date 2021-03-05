#!/bin/bash
ssh root@app.gaevoy.com 'bash -s' <<'ENDSSH'
systemctl stop GaevTodoApp
systemctl disable GaevTodoApp 
rm /etc/systemd/system/GaevTodoApp.service 
systemctl daemon-reload
systemctl reset-failed
rm -rf /apps/GaevTodoApp
ENDSSH