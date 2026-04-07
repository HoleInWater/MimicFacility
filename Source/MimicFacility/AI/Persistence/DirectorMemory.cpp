// DirectorMemory.cpp — Persistent Director memory with JSON save/load.
// Copyright (c) 2026 HoleInWater. All rights reserved.

#include "DirectorMemory.h"
#include "Misc/FileHelper.h"
#include "Misc/Paths.h"
#include "Misc/SecureHash.h"
#include "Serialization/JsonSerializer.h"
#include "Dom/JsonObject.h"
#include "JsonObjectConverter.h"

UDirectorMemory::UDirectorMemory()
{
}

FString UDirectorMemory::ComputeGroupHash(TArray<FString> PlayerIDs)
{
	PlayerIDs.Sort();
	FString Combined = FString::Join(PlayerIDs, TEXT("|"));
	return FMD5::HashAnsiString(*Combined);
}

FString UDirectorMemory::GetSaveFilePath(const FString& GroupHash) const
{
	return FPaths::Combine(FPaths::ProjectSavedDir(), TEXT("DirectorMemory"), GroupHash + TEXT(".json"));
}

void UDirectorMemory::InitializeNewGroup(const TArray<FString>& PlayerIDs, const TArray<FString>& DisplayNames)
{
	Data = FDirectorMemoryData();
	Data.GroupHash = ComputeGroupHash(const_cast<TArray<FString>&>(PlayerIDs));
	Data.PlayerDisplayNames = DisplayNames;

	// Try loading existing data for this group
	if (LoadMemory(Data.GroupHash))
	{
		Data.SessionCount++;
		UE_LOG(LogTemp, Warning, TEXT("DirectorMemory — Returning group detected. Session %d. Corruption: %d"),
			Data.SessionCount, Data.CorruptionIndex);
	}
	else
	{
		Data.SessionCount = 1;
		UE_LOG(LogTemp, Log, TEXT("DirectorMemory — New group. First session."));
	}
}

bool UDirectorMemory::LoadMemory(const FString& GroupHash)
{
	FString FilePath = GetSaveFilePath(GroupHash);

	FString FileContent;
	if (!FFileHelper::LoadFileToString(FileContent, *FilePath))
	{
		return false;
	}

	TSharedPtr<FJsonObject> JsonObj;
	TSharedRef<TJsonReader<>> Reader = TJsonReaderFactory<>::Create(FileContent);
	if (!FJsonSerializer::Deserialize(Reader, JsonObj) || !JsonObj.IsValid())
	{
		UE_LOG(LogTemp, Warning, TEXT("DirectorMemory — Failed to parse save file: %s"), *FilePath);
		return false;
	}

	Data.GroupHash = JsonObj->GetStringField(TEXT("GroupHash"));
	Data.SessionCount = JsonObj->GetIntegerField(TEXT("SessionCount"));
	Data.CorruptionIndex = JsonObj->GetIntegerField(TEXT("CorruptionIndex"));
	Data.LastEnding = static_cast<ESessionEnding>(JsonObj->GetIntegerField(TEXT("LastEnding")));
	Data.TotalPlaytimeSeconds = JsonObj->GetNumberField(TEXT("TotalPlaytimeSeconds"));
	Data.TotalAccusationsMade = JsonObj->GetIntegerField(TEXT("TotalAccusationsMade"));
	Data.TotalFalseAccusations = JsonObj->GetIntegerField(TEXT("TotalFalseAccusations"));
	Data.TotalDirectorQuestionsAnswered = JsonObj->GetIntegerField(TEXT("TotalDirectorQuestionsAnswered"));

	const TArray<TSharedPtr<FJsonValue>>* FactsArray;
	if (JsonObj->TryGetArrayField(TEXT("RememberedFacts"), FactsArray))
	{
		for (const auto& Val : *FactsArray)
		{
			Data.RememberedFacts.Add(Val->AsString());
		}
	}

	const TArray<TSharedPtr<FJsonValue>>* NamesArray;
	if (JsonObj->TryGetArrayField(TEXT("PlayerDisplayNames"), NamesArray))
	{
		for (const auto& Val : *NamesArray)
		{
			Data.PlayerDisplayNames.Add(Val->AsString());
		}
	}

	UE_LOG(LogTemp, Log, TEXT("DirectorMemory — Loaded: %s (sessions: %d, corruption: %d)"),
		*GroupHash, Data.SessionCount, Data.CorruptionIndex);
	return true;
}

bool UDirectorMemory::SaveMemory()
{
	TSharedPtr<FJsonObject> JsonObj = MakeShared<FJsonObject>();
	JsonObj->SetStringField(TEXT("GroupHash"), Data.GroupHash);
	JsonObj->SetNumberField(TEXT("SessionCount"), Data.SessionCount);
	JsonObj->SetNumberField(TEXT("CorruptionIndex"), Data.CorruptionIndex);
	JsonObj->SetNumberField(TEXT("LastEnding"), static_cast<int32>(Data.LastEnding));
	JsonObj->SetNumberField(TEXT("TotalPlaytimeSeconds"), Data.TotalPlaytimeSeconds);
	JsonObj->SetNumberField(TEXT("TotalAccusationsMade"), Data.TotalAccusationsMade);
	JsonObj->SetNumberField(TEXT("TotalFalseAccusations"), Data.TotalFalseAccusations);
	JsonObj->SetNumberField(TEXT("TotalDirectorQuestionsAnswered"), Data.TotalDirectorQuestionsAnswered);

	TArray<TSharedPtr<FJsonValue>> FactsArray;
	for (const FString& Fact : Data.RememberedFacts)
	{
		FactsArray.Add(MakeShared<FJsonValueString>(Fact));
	}
	JsonObj->SetArrayField(TEXT("RememberedFacts"), FactsArray);

	TArray<TSharedPtr<FJsonValue>> NamesArray;
	for (const FString& Name : Data.PlayerDisplayNames)
	{
		NamesArray.Add(MakeShared<FJsonValueString>(Name));
	}
	JsonObj->SetArrayField(TEXT("PlayerDisplayNames"), NamesArray);

	FString OutputString;
	TSharedRef<TJsonWriter<TCHAR, TPrettyJsonPrintPolicy<TCHAR>>> Writer =
		TJsonWriterFactory<TCHAR, TPrettyJsonPrintPolicy<TCHAR>>::Create(&OutputString);
	FJsonSerializer::Serialize(JsonObj.ToSharedRef(), Writer);

	FString FilePath = GetSaveFilePath(Data.GroupHash);
	FString Directory = FPaths::GetPath(FilePath);
	IPlatformFile::GetPlatformPhysical().CreateDirectoryTree(*Directory);

	bool bSaved = FFileHelper::SaveStringToFile(OutputString, *FilePath);
	UE_LOG(LogTemp, Log, TEXT("DirectorMemory — %s save to %s"),
		bSaved ? TEXT("Saved") : TEXT("FAILED"), *FilePath);
	return bSaved;
}

void UDirectorMemory::AddRememberedFact(const FString& Fact)
{
	if (Data.RememberedFacts.Num() < 50) // Cap at 50 facts
	{
		Data.RememberedFacts.Add(Fact);
	}
}

void UDirectorMemory::RecordSessionEnd(ESessionEnding Ending, float SessionDuration,
	int32 Accusations, int32 FalseAccusations, int32 QuestionsAnswered)
{
	Data.LastEnding = Ending;
	Data.TotalPlaytimeSeconds += SessionDuration;
	Data.TotalAccusationsMade += Accusations;
	Data.TotalFalseAccusations += FalseAccusations;
	Data.TotalDirectorQuestionsAnswered += QuestionsAnswered;

	SaveMemory();
}
