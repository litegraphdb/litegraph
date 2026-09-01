from enum import Enum


class ChatProviderType_Enum(str, Enum):
    """
    Chat provider type enum.
    """
    OpenAI = "OpenAI"
    Ollama = "Ollama"
    Gemini = "Gemini"
    Anthropic = "Anthropic"
    VoyageAI = "VoyageAI"
