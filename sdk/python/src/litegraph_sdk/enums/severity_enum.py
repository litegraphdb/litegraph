from enum import Enum


class Severity_Enum(str, Enum):
    """
    Severity Enum
    """
    Debug = "DEBUG"  # 0
    Info = "INFO"  # 1
    Warn = "WARNING"  # 2
    Error = "ERROR"  # 2
    Alert = "ALERT"  # 4
    Critical = "CRITICAL"  # 5
    Emergency = "EMERGENCY"  # 6
