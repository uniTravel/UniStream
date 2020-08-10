namespace UniStream.Infrastructure.EventStore


/// <summary>过滤类型
/// </summary>
/// <typeparam name="StreamPrefix">流名称前缀。</typeparam>
/// <typeparam name="StreamRegular">流名称正则表达式。</typeparam>
/// <typeparam name="EventTypePrefix">事件类型前缀。</typeparam>
/// <typeparam name="EventTypeRegular">事件类型正则表达式。</typeparam>
type FilterType =
    | StreamPrefix of string
    | StreamRegular of string
    | EventTypePrefix of string
    | EventTypeRegular of string