namespace UniStream.Infrastructure.EventStore


/// <summary>过滤类型
/// </summary>
/// <param name="StreamPrefix">流名称前缀。</param>
/// <param name="StreamRegular">流名称正则表达式。</param>
/// <param name="EventTypePrefix">事件类型前缀。</param>
/// <param name="EventTypeRegular">事件类型正则表达式。</param>
type FilterType =
    | StreamPrefix of string
    | StreamRegular of string
    | EventTypePrefix of string
    | EventTypeRegular of string