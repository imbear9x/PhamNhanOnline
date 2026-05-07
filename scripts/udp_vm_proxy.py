#!/usr/bin/env python3
import argparse
import os
import selectors
import signal
import socket
import sys
import time
from typing import Dict, Tuple


ClientEndpoint = Tuple[str, int]


class ProxySession:
    def __init__(self, client: ClientEndpoint, target_host: str, target_port: int) -> None:
        self.client = client
        self.sock = socket.socket(socket.AF_INET, socket.SOCK_DGRAM)
        self.sock.setblocking(False)
        if hasattr(socket, "SIO_UDP_CONNRESET"):
            try:
                self.sock.ioctl(socket.SIO_UDP_CONNRESET, False)
            except OSError:
                pass
        self.sock.connect((target_host, target_port))
        self.last_activity = time.time()

    def close(self) -> None:
        try:
            self.sock.close()
        except OSError:
            pass


def parse_args() -> argparse.Namespace:
    parser = argparse.ArgumentParser(description="Forward UDP traffic from localhost to a VM guest.")
    parser.add_argument("--listen-host", default="127.0.0.1")
    parser.add_argument("--listen-port", type=int, default=7777)
    parser.add_argument("--target-host", default="192.168.192.128")
    parser.add_argument("--target-port", type=int, default=7777)
    parser.add_argument("--idle-seconds", type=int, default=60)
    parser.add_argument("--pid-file", default="")
    return parser.parse_args()


def main() -> int:
    args = parse_args()
    running = True

    def stop_handler(_signum, _frame) -> None:
        nonlocal running
        running = False

    signal.signal(signal.SIGINT, stop_handler)
    signal.signal(signal.SIGTERM, stop_handler)

    listener = socket.socket(socket.AF_INET, socket.SOCK_DGRAM)
    listener.setsockopt(socket.SOL_SOCKET, socket.SO_REUSEADDR, 1)
    listener.bind((args.listen_host, args.listen_port))
    listener.setblocking(False)
    if hasattr(socket, "SIO_UDP_CONNRESET"):
        try:
            listener.ioctl(socket.SIO_UDP_CONNRESET, False)
        except OSError:
            pass

    selector = selectors.DefaultSelector()
    selector.register(listener, selectors.EVENT_READ, data=("listener", None))

    sessions: Dict[ClientEndpoint, ProxySession] = {}
    socket_to_client: Dict[socket.socket, ClientEndpoint] = {}

    if args.pid_file:
        with open(args.pid_file, "w", encoding="ascii") as pid_file:
            pid_file.write(str(os.getpid()))

    print(
        f"UDP proxy ready: {args.listen_host}:{args.listen_port} -> "
        f"{args.target_host}:{args.target_port}",
        flush=True,
    )

    try:
        while running:
            for key, _mask in selector.select(timeout=1.0):
                kind, _ = key.data
                if kind == "listener":
                    payload, client = listener.recvfrom(65535)
                    session = sessions.get(client)
                    if session is None:
                        session = ProxySession(client, args.target_host, args.target_port)
                        sessions[client] = session
                        socket_to_client[session.sock] = client
                        selector.register(session.sock, selectors.EVENT_READ, data=("target", None))
                    session.last_activity = time.time()
                    session.sock.send(payload)
                else:
                    backend_sock = key.fileobj
                    client = socket_to_client[backend_sock]
                    try:
                        payload = backend_sock.recv(65535)
                    except ConnectionResetError:
                        sessions[client].last_activity = time.time()
                        continue
                    listener.sendto(payload, client)
                    sessions[client].last_activity = time.time()

            now = time.time()
            expired = [
                client for client, session in sessions.items()
                if now - session.last_activity > args.idle_seconds
            ]
            for client in expired:
                session = sessions.pop(client)
                socket_to_client.pop(session.sock, None)
                try:
                    selector.unregister(session.sock)
                except Exception:
                    pass
                session.close()
    finally:
        if args.pid_file and os.path.exists(args.pid_file):
            try:
                os.remove(args.pid_file)
            except OSError:
                pass
        for session in sessions.values():
            try:
                selector.unregister(session.sock)
            except Exception:
                pass
            session.close()
        try:
            selector.unregister(listener)
        except Exception:
            pass
        listener.close()

    print("UDP proxy stopped", flush=True)
    return 0


if __name__ == "__main__":
    try:
        raise SystemExit(main())
    except OSError as exc:
        print(f"UDP proxy failed: {exc}", file=sys.stderr, flush=True)
        raise SystemExit(1)
