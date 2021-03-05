#!/bin/bash
ssh root@app.gaevoy.com 'bash -s' <<'ENDSSH'
printf "Stopping service...\n"
systemctl stop GaevTodoApp
printf "Service is "
systemctl is-active GaevTodoApp
mkdir -p /apps/GaevTodoApp
ENDSSH

printf "Uploading new version of service...\n"
rsync -v -a ../bin/Release/net5.0/linux-x64/publish/ root@app.gaevoy.com:/apps/GaevTodoApp/

ssh root@app.gaevoy.com 'bash -s' <<'ENDSSH'
chmod 777 /apps/GaevTodoApp/EventSourcingDoor.Examples.TodoApp
if [[ ! -e /etc/systemd/system/GaevTodoApp.service ]]; then
    printf "Installing service...\n"
    cat > /etc/systemd/system/GaevTodoApp.service <<'EOF'
    [Unit]
    Description=GaevTodoApp
    After=network.target
    
    [Service]
    WorkingDirectory=/apps/GaevTodoApp
    ExecStart=/apps/GaevTodoApp/EventSourcingDoor.Examples.TodoApp
    Restart=always
    KillSignal=SIGINT
    
    [Install]
    WantedBy=multi-user.target
EOF
    systemctl daemon-reload
    systemctl enable GaevTodoApp
fi
printf "Starting service...\n"
systemctl start GaevTodoApp
printf "Service is "
systemctl is-active GaevTodoApp
ENDSSH