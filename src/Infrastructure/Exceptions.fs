namespace UniStream.Domain


exception ValidateException of string


exception WriteException of string * exn


exception ReadException of string * exn


exception ReplayException of string * exn
