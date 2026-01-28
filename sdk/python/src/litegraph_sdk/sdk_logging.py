import logging
from typing import Optional

# Set up a logger for the SDK
logger = logging.getLogger("litegraph_sdk")
logger.setLevel(logging.DEBUG)

# Create a console handler with a default log level
console_handler = logging.StreamHandler()
console_handler.setLevel(logging.INFO)

# Create a formatter
formatter = logging.Formatter(
    "%(asctime)s %(message)s",
    datefmt="%Y-%m-%d %H:%M:%S",
)
console_handler.setFormatter(formatter)

# Add the console handler to the logger
logger.addHandler(console_handler)


def set_log_level(level: Optional[str] = "INFO"):
    """Set the logging level for the SDK logger."""
    if level:
        logger.setLevel(getattr(logging, level.upper(), logging.INFO))
        console_handler.setLevel(getattr(logging, level.upper(), logging.INFO))


def add_file_logging(log_file_path: str, level: Optional[str] = "INFO"):
    """Add file logging to the SDK logger."""
    file_handler = logging.FileHandler(log_file_path)
    file_handler.setLevel(getattr(logging, level.upper(), logging.INFO))
    file_handler.setFormatter(formatter)
    logger.addHandler(file_handler)
    return file_handler


def format_log_message(severity: str, message: str) -> str:
    """Format the log message with severity."""
    return f"[{severity}] {message}"


def log_debug(severity: str, message: str):
    """Log a debug message."""
    logger.debug(format_log_message(severity, message))


def log_info(severity: str, message: str):
    """Log an info message."""
    logger.info(format_log_message(severity, message))


def log_warning(severity: str, message: str):
    """Log a warning message."""
    logger.warning(format_log_message(severity, message))


def log_error(severity: str, message: str):
    """Log an error message."""
    logger.error(format_log_message(severity, message))


def log_critical(severity: str, message: str):
    """Log a critical message."""
    logger.critical(format_log_message(severity, message))
