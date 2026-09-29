"""
Syslog relay for Grafana Alloy.

SyslogLogging stamps RFC3164 frames with a zero-padded day ("Sep 02"); Alloy's strict RFC3164
parser requires space padding ("Sep  2") and drops every frame otherwise.  This relay rewrites
the day field and forwards frames to Alloy: UDP 1514 (LiteGraph server) and UDP 2514 (MCP server).
Remove once SyslogLogging emits compliant timestamps.
"""
import re
import socket
import threading

PATTERN = re.compile(rb'^(<\d+>)([A-Z][a-z]{2}) 0(\d) ')


def relay(listen_port, target_port):
    rx = socket.socket(socket.AF_INET, socket.SOCK_DGRAM)
    rx.bind(('0.0.0.0', listen_port))
    tx = socket.socket(socket.AF_INET, socket.SOCK_DGRAM)
    print('relaying udp', listen_port, '-> alloy:' + str(target_port), flush=True)
    while True:
        data, _ = rx.recvfrom(65535)
        data = PATTERN.sub(rb'\g<1>\g<2>  \g<3> ', data, count=1)
        try:
            tx.sendto(data, ('alloy', target_port))
        except OSError:
            pass


threading.Thread(target=relay, args=(2514, 2514), daemon=True).start()
relay(1514, 1514)
