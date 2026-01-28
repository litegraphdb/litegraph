import logging
import os
import tempfile
from datetime import datetime

import pytest
from litegraph_sdk.enums.severity_enum import Severity_Enum
from litegraph_sdk.sdk_logging import (
    add_file_logging,
    console_handler,
    format_log_message,
    formatter,
    log_critical,
    log_debug,
    log_error,
    log_info,
    log_warning,
    logger,
    set_log_level,
)


@pytest.fixture
def temp_log_file():
    """Create a temporary log file for testing."""
    with tempfile.NamedTemporaryFile(delete=False) as tmp:
        yield tmp.name
    # Clean up after test
    if os.path.exists(tmp.name):
        os.unlink(tmp.name)


@pytest.fixture
def log_capture():
    """Capture log messages for testing."""

    class LogCapture:
        def __init__(self):
            self.messages = []

        def handle(self, record):
            self.messages.append(record.getMessage())

    capture = LogCapture()
    handler = logging.Handler()
    handler.handle = capture.handle
    logger.addHandler(handler)
    yield capture
    logger.removeHandler(handler)


@pytest.fixture(autouse=True)
def reset_logger():
    """Reset logger configuration after each test."""
    original_level = logger.level
    original_handlers = logger.handlers.copy()
    yield
    logger.setLevel(original_level)
    logger.handlers = original_handlers


def test_logger_initialization():
    """Test initial logger configuration."""
    assert logger.name == "litegraph_sdk"
    assert logger.level == logging.DEBUG
    assert console_handler in logger.handlers
    assert console_handler.level == logging.INFO


def test_formatter():
    """Test log message formatter."""
    test_message = "Test message"
    formatted = formatter.format(
        logging.LogRecord("test", logging.INFO, "", 0, test_message, None, None)
    )
    assert test_message in formatted
    assert datetime.now().strftime("%Y-%m-%d") in formatted


def test_set_log_level():
    """Test setting different log levels."""
    log_levels = ["DEBUG", "INFO", "WARNING", "ERROR", "CRITICAL"]

    for level in log_levels:
        set_log_level(level)
        assert logger.level == getattr(logging, level)
        assert console_handler.level == getattr(logging, level)


def test_set_invalid_log_level():
    """Test setting invalid log level defaults to INFO."""
    set_log_level("INVALID_LEVEL")
    assert logger.level == logging.INFO
    assert console_handler.level == logging.INFO


def test_add_file_logging(temp_log_file):
    """Test adding file logging."""
    file_handler = add_file_logging(temp_log_file)

    try:
        # Verify handler was added
        assert file_handler in logger.handlers
        assert file_handler.level == logging.INFO
        assert isinstance(file_handler, logging.FileHandler)
        assert file_handler.formatter == formatter

        # Test logging to file
        test_message = "Test file logging"
        log_info(Severity_Enum.Info.value, test_message)

        # Verify message was written to file
        with open(temp_log_file, "r") as f:
            log_content = f.read()
            assert test_message in log_content
    finally:
        logger.removeHandler(file_handler)


def test_format_log_message():
    """Test log message formatting."""
    severity = Severity_Enum.Error.value
    message = "Test message"
    formatted = format_log_message(severity, message)
    assert formatted == f"[{severity}] {message}"


@pytest.mark.parametrize(
    "log_func,severity,level",
    [
        (log_debug, Severity_Enum.Debug.value, logging.DEBUG),
        (log_info, Severity_Enum.Info.value, logging.INFO),
        (log_warning, Severity_Enum.Warn.value, logging.WARNING),
        (log_error, Severity_Enum.Error.value, logging.ERROR),
        (log_critical, Severity_Enum.Critical.value, logging.CRITICAL),
    ],
)
def test_log_levels(log_func, severity, level, log_capture):
    """Test different logging levels."""
    # Set logger to capture all messages
    logger.setLevel(logging.DEBUG)

    test_message = f"Test {severity} message"
    log_func(severity, test_message)

    assert len(log_capture.messages) == 1
    expected_message = f"[{severity}] {test_message}"
    assert expected_message in log_capture.messages[0]


def test_log_level_filtering(log_capture):
    """Test log level filtering."""
    # Set to WARNING level
    set_log_level("WARNING")

    # Send messages at different levels
    log_debug(Severity_Enum.Debug.value, "Debug message")
    log_info(Severity_Enum.Info.value, "Info message")
    log_warning(Severity_Enum.Warn.value, "Warning message")
    log_error(Severity_Enum.Error.value, "Error message")

    # Should only capture WARNING and above
    assert len([msg for msg in log_capture.messages if "Debug message" in msg]) == 0
    assert len([msg for msg in log_capture.messages if "Info message" in msg]) == 0
    assert len([msg for msg in log_capture.messages if "Warning message" in msg]) == 1
    assert len([msg for msg in log_capture.messages if "Error message" in msg]) == 1


def test_multiple_handlers(temp_log_file, log_capture):
    """Test logging to multiple handlers."""
    file_handler = add_file_logging(temp_log_file)

    try:
        test_message = "Test multiple handlers"
        log_info(Severity_Enum.Info.value, test_message)

        # Check console output
        assert test_message in log_capture.messages[0]

        # Check file output
        with open(temp_log_file, "r") as f:
            log_content = f.read()
            assert test_message in log_content
    finally:
        logger.removeHandler(file_handler)


def test_log_formatting_special_characters():
    """Test logging messages with special characters."""
    special_messages = [
        "Message with spaces and symbols: !@#$%^&*()",
        "Multi\nline\nmessage",
        "Message with unicode: 你好",
        "Message with quotes: 'single' and \"double\"",
    ]

    for message in special_messages:
        formatted = format_log_message(Severity_Enum.Info.value, message)
        assert message in formatted


def test_file_handler_permissions(temp_log_file):
    """Test file handler with different permission scenarios."""
    # Test with directory that doesn't exist
    with pytest.raises(Exception):
        add_file_logging("/nonexistent/directory/log.txt")

    # Test with valid file
    handler = add_file_logging(temp_log_file)
    try:
        assert isinstance(handler, logging.FileHandler)
    finally:
        logger.removeHandler(handler)


def test_severity_enum_values():
    """Test that severity enum values are handled correctly."""
    for severity in Severity_Enum:
        message = f"Test message for {severity.name}"
        formatted = format_log_message(severity.value, message)
        assert f"[{severity.value}]" in formatted
        assert message in formatted
