// PromptBuilder.h — Assembles game state context into LLM system prompts for Director and Mimic dialogue.
// Copyright (c) 2026 HoleInWater. All rights reserved.

#pragma once

#include "CoreMinimal.h"
#include "UObject/NoExportTypes.h"
#include "OllamaClient.h"
#include "PromptBuilder.generated.h"

UENUM(BlueprintType)
enum class EDirectorPhase : uint8
{
	Helpful         UMETA(DisplayName = "Helpful"),
	Revealing       UMETA(DisplayName = "Revealing"),
	Manipulative    UMETA(DisplayName = "Manipulative"),
	Confrontational UMETA(DisplayName = "Confrontational"),
	Transcendent    UMETA(DisplayName = "Transcendent")
};

USTRUCT(BlueprintType)
struct FDirectorContext
{
	GENERATED_BODY()

	UPROPERTY(BlueprintReadWrite)
	EDirectorPhase Phase;

	UPROPERTY(BlueprintReadWrite)
	int32 RoundNumber;

	UPROPERTY(BlueprintReadWrite)
	int32 ActiveMimicCount;

	UPROPERTY(BlueprintReadWrite)
	int32 ContainedMimicCount;

	UPROPERTY(BlueprintReadWrite)
	int32 LivingPlayerCount;

	UPROPERTY(BlueprintReadWrite)
	int32 CorruptionIndex;

	UPROPERTY(BlueprintReadWrite)
	int32 SessionCount;

	UPROPERTY(BlueprintReadWrite)
	FString LastEvent;

	// Personal weapon data
	UPROPERTY(BlueprintReadWrite)
	FString TargetPlayerPhrases;

	UPROPERTY(BlueprintReadWrite)
	FString SocialDynamicsSummary;

	UPROPERTY(BlueprintReadWrite)
	FString VerbalSlipToUse;

	UPROPERTY(BlueprintReadWrite)
	FString EmotionalProfileSummary;

	FDirectorContext()
		: Phase(EDirectorPhase::Helpful)
		, RoundNumber(1)
		, ActiveMimicCount(0)
		, ContainedMimicCount(0)
		, LivingPlayerCount(4)
		, CorruptionIndex(0)
		, SessionCount(1)
	{}
};

USTRUCT(BlueprintType)
struct FMimicContext
{
	GENERATED_BODY()

	UPROPERTY(BlueprintReadWrite)
	FString TargetPlayerName;

	UPROPERTY(BlueprintReadWrite)
	int32 SubjectNumber;

	UPROPERTY(BlueprintReadWrite)
	FString PhraseList;

	UPROPERTY(BlueprintReadWrite)
	FString WitnessedPhrases;

	UPROPERTY(BlueprintReadWrite)
	FString UnwitnessedPhrases;

	UPROPERTY(BlueprintReadWrite)
	FString SituationContext;

	FMimicContext() : SubjectNumber(1) {}
};

UCLASS(BlueprintType)
class MIMICFACILITY_API UPromptBuilder : public UObject
{
	GENERATED_BODY()

public:
	UPromptBuilder();

	UFUNCTION(BlueprintCallable, Category = "LLM|Prompt")
	FLLMRequest BuildDirectorRequest(const FDirectorContext& Context) const;

	UFUNCTION(BlueprintCallable, Category = "LLM|Prompt")
	FLLMRequest BuildMimicRequest(const FMimicContext& Context) const;

	UFUNCTION(BlueprintCallable, Category = "LLM|Prompt")
	void SetModel(const FString& ModelName);

private:
	FString BuildDirectorSystemPrompt(const FDirectorContext& Context) const;
	FString BuildMimicSystemPrompt(const FMimicContext& Context) const;
	FString PhaseToString(EDirectorPhase Phase) const;
	FString GetCorruptionInternalMonologue(int32 CorruptionIndex) const;

	FString ModelName;
};
