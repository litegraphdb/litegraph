#!/bin/bash

rm -rf litegraph.json

rm -rf logs/*
rm -rf temp/*
rm -rf backups/*

rmdir logs 2>/dev/null || true
rmdir temp 2>/dev/null || true
rmdir backups 2>/dev/null || true

if [ -d "bin" ]; then rm -rf bin; fi
if [ -d "obj" ]; then rm -rf obj; fi