namespace UniStream.Domain


exception ValidateException of message: string


exception WriteException of message: string * inner: exn


exception ReadException of message: string * inner: exn


exception ReplayException of message: string * inner: exn


exception RegisterException of message: string
