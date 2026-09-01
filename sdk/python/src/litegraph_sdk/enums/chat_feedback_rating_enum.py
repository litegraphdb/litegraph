from enum import Enum


class ChatFeedbackRating_Enum(str, Enum):
    """
    Chat feedback rating enum.
    """
    ThumbsUp = "ThumbsUp"
    ThumbsDown = "ThumbsDown"
