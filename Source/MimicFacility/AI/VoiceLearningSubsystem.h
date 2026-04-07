// VoiceLearningSubsystem.h — Captures, processes, and stores player voice data for Mimic impersonation.
// Copyright (c) 2026 HoleInWater. All rights reserved.

#pragma once

#include "CoreMinimal.h"
#include "Subsystems/GameInstanceSubsystem.h"
#include "VoiceLearningSubsystem.generated.h"

/**
 * FVoicePhrase
 * A single captured phrase from a player's voice chat.
 */
USTRUCT(BlueprintType)
struct FVoicePhrase
{
	GENERATED_BODY()

	/** Transcribed text of the phrase. */
	UPROPERTY(BlueprintReadOnly)
	FString Text;

	/** Timestamp within the session when this phrase was captured. */
	UPROPERTY(BlueprintReadOnly)
	float SessionTimestamp;

	/** IDs of other players who were within earshot when this phrase was spoken. */
	UPROPERTY(BlueprintReadOnly)
	TArray<FString> WitnessPlayerIDs;
};

/**
 * UVoiceLearningSubsystem
 * Game instance subsystem that passively captures player voice chat during Round 1,
 * processes it into phrase data, selects Trigger Words, and provides voice profiles
 * to Mimic actors for playback in Round 2+.
 */
UCLASS()
class MIMICFACILITY_API UVoiceLearningSubsystem : public UGameInstanceSubsystem
{
	GENERATED_BODY()

public:
	virtual void Initialize(FSubsystemCollectionBase& Collection) override;
	virtual void Deinitialize() override;

	/** Get all captured phrases for a specific player. */
	UFUNCTION(BlueprintCallable, Category = "Voice")
	TArray<FVoicePhrase> GetPhrasesForPlayer(const FString& PlayerID) const;

	/** Get the assigned trigger words for a specific player. */
	UFUNCTION(BlueprintCallable, Category = "Voice")
	TArray<FString> GetTriggerWords(const FString& PlayerID) const;

protected:
	/** Per-player phrase database. */
	TMap<FString, TArray<FVoicePhrase>> PhraseDatabase;

	/** Per-player trigger word assignments (selected at end of Round 1). */
	TMap<FString, TArray<FString>> TriggerWordAssignments;
};
