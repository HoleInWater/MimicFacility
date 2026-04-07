// DirectorMemory.h — Persistent session data for The Director. Survives between sessions per player group.
// Copyright (c) 2026 HoleInWater. All rights reserved.

#pragma once

#include "CoreMinimal.h"
#include "UObject/NoExportTypes.h"
#include "DirectorMemory.generated.h"

UENUM(BlueprintType)
enum class ESessionEnding : uint8
{
	None,
	Escaped,
	StayedTooLong,
	AMEnding
};

USTRUCT(BlueprintType)
struct FDirectorMemoryData
{
	GENERATED_BODY()

	UPROPERTY(BlueprintReadWrite)
	FString GroupHash;

	UPROPERTY(BlueprintReadWrite)
	int32 SessionCount;

	UPROPERTY(BlueprintReadWrite)
	int32 CorruptionIndex;

	UPROPERTY(BlueprintReadWrite)
	TArray<FString> RememberedFacts;

	UPROPERTY(BlueprintReadWrite)
	TArray<FString> UnaskedQuestions;

	UPROPERTY(BlueprintReadWrite)
	TArray<FString> PlayerDisplayNames;

	UPROPERTY(BlueprintReadWrite)
	ESessionEnding LastEnding;

	UPROPERTY(BlueprintReadWrite)
	float TotalPlaytimeSeconds;

	UPROPERTY(BlueprintReadWrite)
	int32 TotalAccusationsMade;

	UPROPERTY(BlueprintReadWrite)
	int32 TotalFalseAccusations;

	UPROPERTY(BlueprintReadWrite)
	int32 TotalDirectorQuestionsAnswered;

	FDirectorMemoryData()
		: SessionCount(0)
		, CorruptionIndex(0)
		, LastEnding(ESessionEnding::None)
		, TotalPlaytimeSeconds(0.0f)
		, TotalAccusationsMade(0)
		, TotalFalseAccusations(0)
		, TotalDirectorQuestionsAnswered(0)
	{}
};

UCLASS(BlueprintType)
class MIMICFACILITY_API UDirectorMemory : public UObject
{
	GENERATED_BODY()

public:
	UDirectorMemory();

	UFUNCTION(BlueprintCallable, Category = "Director|Memory")
	bool LoadMemory(const FString& GroupHash);

	UFUNCTION(BlueprintCallable, Category = "Director|Memory")
	bool SaveMemory();

	UFUNCTION(BlueprintCallable, Category = "Director|Memory")
	void InitializeNewGroup(const TArray<FString>& PlayerIDs, const TArray<FString>& DisplayNames);

	UFUNCTION(BlueprintCallable, Category = "Director|Memory")
	void AddRememberedFact(const FString& Fact);

	UFUNCTION(BlueprintCallable, Category = "Director|Memory")
	void RecordSessionEnd(ESessionEnding Ending, float SessionDuration, int32 Accusations, int32 FalseAccusations, int32 QuestionsAnswered);

	UFUNCTION(BlueprintPure, Category = "Director|Memory")
	FDirectorMemoryData GetMemoryData() const { return Data; }

	UFUNCTION(BlueprintPure, Category = "Director|Memory")
	bool IsReturningGroup() const { return Data.SessionCount > 0; }

	UFUNCTION(BlueprintPure, Category = "Director|Memory")
	int32 GetSessionCount() const { return Data.SessionCount; }

	UFUNCTION(BlueprintCallable, Category = "Director|Memory")
	static FString ComputeGroupHash(TArray<FString> PlayerIDs);

private:
	FString GetSaveFilePath(const FString& GroupHash) const;

	UPROPERTY()
	FDirectorMemoryData Data;
};
