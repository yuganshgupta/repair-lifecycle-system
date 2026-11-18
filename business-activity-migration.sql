BEGIN TRANSACTION;
CREATE TABLE [dbo].[BusinessActivityEvents] (
    [BusinessActivityEventId] int NOT NULL IDENTITY,
    [EventType] int NOT NULL,
    [OccurredAtUtc] datetime2 NOT NULL,
    [ActorUserId] int NULL,
    [ActorName] nvarchar(120) NOT NULL,
    [ActorRole] nvarchar(32) NOT NULL,
    [TargetUserId] int NULL,
    [TargetUserName] nvarchar(120) NULL,
    [CustomerId] int NULL,
    [CustomerName] nvarchar(120) NULL,
    [ComplaintId] int NULL,
    [TicketNumber] nvarchar(64) NULL,
    [Title] nvarchar(120) NOT NULL,
    [Description] nvarchar(1000) NOT NULL,
    [DetailsJson] nvarchar(max) NULL,
    CONSTRAINT [PK_BusinessActivityEvents] PRIMARY KEY ([BusinessActivityEventId])
);

CREATE INDEX [IX_BusinessActivityEvents_ActorUserId_OccurredAtUtc] ON [dbo].[BusinessActivityEvents] ([ActorUserId], [OccurredAtUtc]);

CREATE INDEX [IX_BusinessActivityEvents_ComplaintId_OccurredAtUtc] ON [dbo].[BusinessActivityEvents] ([ComplaintId], [OccurredAtUtc]);

CREATE INDEX [IX_BusinessActivityEvents_CustomerId_OccurredAtUtc] ON [dbo].[BusinessActivityEvents] ([CustomerId], [OccurredAtUtc]);

CREATE INDEX [IX_BusinessActivityEvents_EventType_OccurredAtUtc] ON [dbo].[BusinessActivityEvents] ([EventType], [OccurredAtUtc]);

CREATE INDEX [IX_BusinessActivityEvents_OccurredAtUtc] ON [dbo].[BusinessActivityEvents] ([OccurredAtUtc]);

INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
VALUES (N'20261117220453_AddBusinessActivityEvents', N'10.0.13');

COMMIT;
GO

