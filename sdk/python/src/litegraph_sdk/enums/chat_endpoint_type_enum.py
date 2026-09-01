from enum import Enum


class ChatEndpointType_Enum(str, Enum):
    """
    Chat endpoint type enum.
    """
    Embedding = "Embedding"
    Completion = "Completion"
