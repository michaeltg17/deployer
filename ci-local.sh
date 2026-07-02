#!/bin/bash
docker build -f Dockerfile.ci -t deployer-ci .
docker run --rm -v /var/run/docker.sock:/var/run/docker.sock deployer-ci
