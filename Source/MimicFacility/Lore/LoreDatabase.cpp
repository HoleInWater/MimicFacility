// LoreDatabase.cpp — Persistent lore storage subsystem with corruption-gated entry reveal across three narrative channels.
// Copyright (c) 2026 HoleInWater. All rights reserved.

#include "LoreDatabase.h"
#include "Misc/FileHelper.h"
#include "Serialization/JsonReader.h"
#include "Serialization/JsonSerializer.h"

void ULoreDatabase::Initialize(FSubsystemCollectionBase& Collection)
{
	Super::Initialize(Collection);
	LoadEntriesFromJSON();
}

void ULoreDatabase::LoadEntriesFromJSON()
{
	FString JsonPath = FPaths::ProjectContentDir() / TEXT("Data/LoreEntries.json");
	FString JsonString;

	if (!FFileHelper::LoadFileToString(JsonString, *JsonPath))
	{
		UE_LOG(LogTemp, Warning, TEXT("LoreDatabase: Failed to load %s"), *JsonPath);
		return;
	}

	TSharedPtr<FJsonValue> RootValue;
	TSharedRef<TJsonReader<>> Reader = TJsonReaderFactory<>::Create(JsonString);
	if (!FJsonSerializer::Deserialize(Reader, RootValue) || !RootValue.IsValid())
	{
		UE_LOG(LogTemp, Error, TEXT("LoreDatabase: Failed to parse JSON"));
		return;
	}

	const TArray<TSharedPtr<FJsonValue>>& EntriesArray = RootValue->AsArray();
	for (const TSharedPtr<FJsonValue>& EntryValue : EntriesArray)
	{
		const TSharedPtr<FJsonObject>& Obj = EntryValue->AsObject();
		if (!Obj.IsValid())
		{
			continue;
		}

		FLoreEntry Entry;
		Entry.EntryID = FName(*Obj->GetStringField(TEXT("EntryID")));
		Entry.TerminalID = FName(*Obj->GetStringField(TEXT("TerminalID")));
		Entry.Title = Obj->GetStringField(TEXT("Title"));
		Entry.Content = Obj->GetStringField(TEXT("Content"));
		Entry.Author = Obj->GetStringField(TEXT("Author"));
		Entry.Classification = Obj->GetStringField(TEXT("Classification"));
		Entry.MinCorruptionToReveal = Obj->GetIntegerField(TEXT("MinCorruptionToReveal"));
		Entry.bIsRedacted = Obj->GetBoolField(TEXT("bIsRedacted"));
		Entry.RedactedContent = Obj->GetStringField(TEXT("RedactedContent"));

		FString ChannelStr = Obj->GetStringField(TEXT("Channel"));
		if (ChannelStr == TEXT("Environmental"))
		{
			Entry.Channel = ELoreChannel::Environmental;
		}
		else if (ChannelStr == TEXT("Director"))
		{
			Entry.Channel = ELoreChannel::Director;
		}
		else
		{
			Entry.Channel = ELoreChannel::Terminal;
		}

		AllEntries.Add(Entry);
		TerminalEntries.FindOrAdd(Entry.TerminalID).Add(Entry);
	}

	UE_LOG(LogTemp, Log, TEXT("LoreDatabase: Loaded %d entries from JSON"), AllEntries.Num());
}

TArray<FLoreEntry> ULoreDatabase::GetEntriesForTerminal(FName TerminalID, int32 CorruptionLevel) const
{
	TArray<FLoreEntry> Results;

	const TArray<FLoreEntry>* Found = TerminalEntries.Find(TerminalID);
	if (!Found)
	{
		return Results;
	}

	for (const FLoreEntry& Entry : *Found)
	{
		if (Entry.MinCorruptionToReveal <= CorruptionLevel)
		{
			Results.Add(Entry);
		}
	}

	return Results;
}

TArray<FLoreEntry> ULoreDatabase::GetEnvironmentalLore(FName ZoneTag) const
{
	TArray<FLoreEntry> Results;

	for (const FLoreEntry& Entry : AllEntries)
	{
		if (Entry.Channel == ELoreChannel::Environmental && Entry.TerminalID == ZoneTag)
		{
			Results.Add(Entry);
		}
	}

	return Results;
}

TArray<FLoreEntry> ULoreDatabase::GetDirectorLore(int32 CorruptionLevel) const
{
	TArray<FLoreEntry> Results;

	for (const FLoreEntry& Entry : AllEntries)
	{
		if (Entry.Channel == ELoreChannel::Director && Entry.MinCorruptionToReveal <= CorruptionLevel)
		{
			Results.Add(Entry);
		}
	}

	return Results;
}

void ULoreDatabase::MarkEntryAsRead(FName EntryID)
{
	ReadEntries.Add(EntryID);
}

int32 ULoreDatabase::GetUnreadCount() const
{
	int32 Count = 0;
	for (const FLoreEntry& Entry : AllEntries)
	{
		if (!ReadEntries.Contains(Entry.EntryID))
		{
			Count++;
		}
	}
	return Count;
}

bool ULoreDatabase::IsEntryRead(FName EntryID) const
{
	return ReadEntries.Contains(EntryID);
}
