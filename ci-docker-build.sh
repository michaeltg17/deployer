#!/bin/bash
set -e

exec docker compose -f docker-compose.ci.yml build
