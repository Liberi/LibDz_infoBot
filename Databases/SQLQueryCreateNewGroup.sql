CREATE TABLE [dbo].[NewGroup_Calls] (
    [lesson_number] INT IDENTITY(1,1) NOT NULL,
    [time_interval] NVARCHAR(100) NOT NULL,
    [note] NVARCHAR(255) NULL,
    CONSTRAINT [PK_NewGroup_Calls] PRIMARY KEY CLUSTERED ([lesson_number] ASC)
);

CREATE TABLE [dbo].[NewGroup_Homework] (
    [id_Homework] INT IDENTITY(1,1) NOT NULL,
    [lesson_name] NVARCHAR(100) NOT NULL,
    [homework] NVARCHAR(1000) NULL,
    CONSTRAINT [PK_NewGroup_Homework] PRIMARY KEY CLUSTERED ([id_Homework] ASC)
);

CREATE TABLE [dbo].[NewGroup_Friday_Schedule] (
    [Id_lesson] INT IDENTITY(1,1) NOT NULL,
    [lesson_name] NVARCHAR(100) NOT NULL,
    [audience_code] NVARCHAR(50) NULL,
    CONSTRAINT [PK_NewGroup_Friday_Schedule] PRIMARY KEY CLUSTERED ([Id_lesson] ASC)
);

CREATE TABLE [dbo].[NewGroup_Monday_Schedule] (
    [Id_lesson] INT IDENTITY(1,1) NOT NULL,
    [lesson_name] NVARCHAR(100) NOT NULL,
    [audience_code] NVARCHAR(50) NULL,
    CONSTRAINT [PK_NewGroup_Monday_Schedule] PRIMARY KEY CLUSTERED ([Id_lesson] ASC)
);

CREATE TABLE [dbo].[NewGroup_Saturday_Schedule] (
    [Id_lesson] INT IDENTITY(1,1) NOT NULL,
    [lesson_name] NVARCHAR(100) NOT NULL,
    [audience_code] NVARCHAR(50) NULL,
    CONSTRAINT [PK_NewGroup_Saturday_Schedule] PRIMARY KEY CLUSTERED ([Id_lesson] ASC)
);

CREATE TABLE [dbo].[NewGroup_Thursday_Schedule] (
    [Id_lesson] INT IDENTITY(1,1) NOT NULL,
    [lesson_name] NVARCHAR(100) NOT NULL,
    [audience_code] NVARCHAR(50) NULL,
    CONSTRAINT [PK_NewGroup_Thursday_Schedule] PRIMARY KEY CLUSTERED ([Id_lesson] ASC)
);

CREATE TABLE [dbo].[NewGroup_Tuesday_Schedule] (
    [Id_lesson] INT IDENTITY(1,1) NOT NULL,
    [lesson_name] NVARCHAR(100) NOT NULL,
    [audience_code] NVARCHAR(50) NULL,
    CONSTRAINT [PK_NewGroup_Tuesday_Schedule] PRIMARY KEY CLUSTERED ([Id_lesson] ASC)
);

CREATE TABLE [dbo].[NewGroup_Wednesday_Schedule] (
    [Id_lesson] INT IDENTITY(1,1) NOT NULL,
    [lesson_name] NVARCHAR(100) NOT NULL,
    [audience_code] NVARCHAR(50) NULL,
    CONSTRAINT [PK_NewGroup_Wednesday_Schedule] PRIMARY KEY CLUSTERED ([Id_lesson] ASC)
);