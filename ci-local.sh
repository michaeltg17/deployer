#!/bin/bash
docker build -f Dockerfile.ci -t deployer-ci .
docker run --rm deployer-ci
