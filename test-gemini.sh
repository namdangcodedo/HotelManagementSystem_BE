#!/bin/bash

echo "Testing Gemini API Key 1 with gemini-2.5-flash..."
curl -X POST "https://generativelanguage.googleapis.com/v1beta/models/gemini-2.5-flash:generateContent?key=AIzaSyBLzuTObWlZHqZMXcfEAfNb8qLwnfNk0zU" \
  -H 'Content-Type: application/json' \
  -d '{"contents":[{"parts":[{"text":"Hello"}]}]}' \
  2>&1 | head -20

echo ""
echo "---"
echo "Testing Gemini API Key 2 with gemini-2.5-flash..."
curl -X POST "https://generativelanguage.googleapis.com/v1beta/models/gemini-2.5-flash:generateContent?key=AIzaSyAic4Wem2qrZg0NkKa8VtVN7jyJ5aaKx6k" \
  -H 'Content-Type: application/json' \
  -d '{"contents":[{"parts":[{"text":"Hello"}]}]}' \
  2>&1 | head -20
