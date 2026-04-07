// PersonalWeaponSystem.h — The Director's four data weapons: voice patterns, emotional profiles, social dynamics, verbal slips.
// Copyright (c) 2026 HoleInWater. All rights reserved.

#pragma once

#include "CoreMinimal.h"
#include "UObject/NoExportTypes.h"
#include "PersonalWeaponSystem.generated.h"

// === Weapon 1: Voice Patterns ===

USTRUCT(BlueprintType)
struct FVoiceProfile
{
	GENERATED_BODY()

	UPROPERTY(BlueprintReadOnly)
	FString PlayerID;

	UPROPERTY(BlueprintReadOnly)
	TArray<FString> TopPhrases;

	UPROPERTY(BlueprintReadOnly)
	TArray<FString> VerbalFillers;

	UPROPERTY(BlueprintReadOnly)
	float AverageSpeechRate;

	UPROPERTY(BlueprintReadOnly)
	float PitchBaseline;

	UPROPERTY(BlueprintReadOnly)
	float PitchUnderStress;

	FVoiceProfile() : AverageSpeechRate(0.0f), PitchBaseline(0.0f), PitchUnderStress(0.0f) {}
};

// === Weapon 2: Emotional Responses ===

UENUM(BlueprintType)
enum class EEmotionalEvent : uint8
{
	Laughter,
	Panic,
	Silence,
	Frustration,
	Relief,
	Confusion
};

USTRUCT(BlueprintType)
struct FEmotionalEntry
{
	GENERATED_BODY()

	UPROPERTY(BlueprintReadOnly)
	EEmotionalEvent EmotionType;

	UPROPERTY(BlueprintReadOnly)
	FString Context;

	UPROPERTY(BlueprintReadOnly)
	float Timestamp;

	UPROPERTY(BlueprintReadOnly)
	FString LocationInFacility;

	FEmotionalEntry() : EmotionType(EEmotionalEvent::Silence), Timestamp(0.0f) {}
};

USTRUCT(BlueprintType)
struct FEmotionalProfile
{
	GENERATED_BODY()

	UPROPERTY(BlueprintReadOnly)
	FString PlayerID;

	UPROPERTY(BlueprintReadOnly)
	TArray<FEmotionalEntry> EventLog;

	UPROPERTY(BlueprintReadOnly)
	float FrustrationThreshold;

	UPROPERTY(BlueprintReadOnly)
	float PanicFrequency;

	FEmotionalProfile() : FrustrationThreshold(0.0f), PanicFrequency(0.0f) {}
};

// === Weapon 3: Social Dynamics ===

USTRUCT(BlueprintType)
struct FSocialMap
{
	GENERATED_BODY()

	UPROPERTY(BlueprintReadOnly)
	FString LeaderID;

	UPROPERTY(BlueprintReadOnly)
	FString MostTrustedPairA;
	UPROPERTY(BlueprintReadOnly)
	FString MostTrustedPairB;

	UPROPERTY(BlueprintReadOnly)
	FString MostVolatilePairA;
	UPROPERTY(BlueprintReadOnly)
	FString MostVolatilePairB;

	UPROPERTY(BlueprintReadOnly)
	FString IsolatedPlayerID;

	UPROPERTY(BlueprintReadOnly)
	TArray<FString> RescuePriority;

	UPROPERTY(BlueprintReadOnly)
	TMap<FString, int32> DeferenceCount;

	UPROPERTY(BlueprintReadOnly)
	TMap<FString, int32> InitiationCount;
};

// === Weapon 4: Verbal Slips ===

UENUM(BlueprintType)
enum class ESlipCategory : uint8
{
	RealName,
	MetaCommentary,
	PersonalReference,
	InJoke
};

USTRUCT(BlueprintType)
struct FVerbalSlip
{
	GENERATED_BODY()

	UPROPERTY(BlueprintReadOnly)
	FString PlayerID;

	UPROPERTY(BlueprintReadOnly)
	FString Phrase;

	UPROPERTY(BlueprintReadOnly)
	FString Context;

	UPROPERTY(BlueprintReadOnly)
	TArray<FString> WitnessIDs;

	UPROPERTY(BlueprintReadOnly)
	ESlipCategory Category;

	UPROPERTY(BlueprintReadOnly)
	bool bHasBeenUsed;

	FVerbalSlip() : Category(ESlipCategory::PersonalReference), bHasBeenUsed(false) {}
};

// === The System ===

UCLASS(BlueprintType)
class MIMICFACILITY_API UPersonalWeaponSystem : public UObject
{
	GENERATED_BODY()

public:
	UPersonalWeaponSystem();

	// Voice Profiles
	UFUNCTION(BlueprintCallable, Category = "Weapons|Voice")
	void RegisterVoiceProfile(const FVoiceProfile& Profile);

	UFUNCTION(BlueprintPure, Category = "Weapons|Voice")
	FVoiceProfile GetVoiceProfile(const FString& PlayerID) const;

	// Emotional Tracking
	UFUNCTION(BlueprintCallable, Category = "Weapons|Emotion")
	void RecordEmotionalEvent(const FString& PlayerID, EEmotionalEvent Type, const FString& Context, const FString& Location);

	UFUNCTION(BlueprintPure, Category = "Weapons|Emotion")
	FEmotionalProfile GetEmotionalProfile(const FString& PlayerID) const;

	UFUNCTION(BlueprintPure, Category = "Weapons|Emotion")
	EEmotionalEvent GetPrimaryFear(const FString& PlayerID) const;

	// Social Dynamics
	UFUNCTION(BlueprintCallable, Category = "Weapons|Social")
	void RecordDeference(const FString& DeferringPlayerID, const FString& DeferredToPlayerID);

	UFUNCTION(BlueprintCallable, Category = "Weapons|Social")
	void RecordRescue(const FString& RescuerID, const FString& RescuedID);

	UFUNCTION(BlueprintCallable, Category = "Weapons|Social")
	void RecordDisagreement(const FString& PlayerA, const FString& PlayerB);

	UFUNCTION(BlueprintCallable, Category = "Weapons|Social")
	void RecordProximity(const FString& PlayerA, const FString& PlayerB, float Duration);

	UFUNCTION(BlueprintCallable, Category = "Weapons|Social")
	void ComputeSocialMap();

	UFUNCTION(BlueprintPure, Category = "Weapons|Social")
	FSocialMap GetSocialMap() const { return SocialDynamics; }

	UFUNCTION(BlueprintPure, Category = "Weapons|Social")
	FString GetMimicTargetPlayer() const;

	// Verbal Slips
	UFUNCTION(BlueprintCallable, Category = "Weapons|Slips")
	void RecordVerbalSlip(const FVerbalSlip& Slip);

	UFUNCTION(BlueprintCallable, Category = "Weapons|Slips")
	FVerbalSlip ConsumeNextSlip();

	UFUNCTION(BlueprintPure, Category = "Weapons|Slips")
	int32 GetUnusedSlipCount() const;

	// Summary generation for LLM prompts
	UFUNCTION(BlueprintPure, Category = "Weapons")
	FString GenerateSocialSummary() const;

	UFUNCTION(BlueprintPure, Category = "Weapons")
	FString GenerateEmotionalSummary(const FString& PlayerID) const;

private:
	UPROPERTY()
	TMap<FString, FVoiceProfile> VoiceProfiles;

	UPROPERTY()
	TMap<FString, FEmotionalProfile> EmotionalProfiles;

	UPROPERTY()
	FSocialMap SocialDynamics;

	UPROPERTY()
	TArray<FVerbalSlip> VerbalSlips;

	// Proximity accumulator for social map computation
	TMap<FString, float> ProximityScores;
	TMap<FString, int32> DisagreementCounts;

	float LastSlipUseTime;
	static constexpr float SlipCooldownSeconds = 300.0f; // 5 minutes
};
