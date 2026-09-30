"""Credentials never appear in the SDK's request log."""

from litegraph_sdk.base import _redact_headers


def test_redacts_credential_headers():
    headers = {
        "Authorization": "Bearer secret-token",
        "x-token": "session-token",
        "Content-Type": "application/json",
    }
    redacted = _redact_headers(headers)
    assert redacted["Authorization"] == "***"
    assert redacted["x-token"] == "***"
    assert redacted["Content-Type"] == "application/json"
    assert headers["Authorization"] == "Bearer secret-token"


def test_redact_handles_missing_headers():
    assert _redact_headers(None) == {}
