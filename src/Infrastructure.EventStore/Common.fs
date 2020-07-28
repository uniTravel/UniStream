namespace UniStream.Infrastructure.EventStore


type FilterType =
    | StreamPrefix of string
    | StreamRegular of string
    | EventTypePrefix of string
    | EventTypeRegular of string