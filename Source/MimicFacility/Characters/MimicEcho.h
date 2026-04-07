// MimicEcho.h — Invisible mimic that replays Round 1 conversations in empty rooms. Psychological threat only.
// Copyright (c) 2026 HoleInWater. All rights reserved.

#pragma once

#include "CoreMinimal.h"
#include "GameFramework/Actor.h"
#include "MimicEcho.generated.h"

class UAudioComponent;

UCLASS()
class MIMICFACILITY_API AMimicEcho : public AActor
{
	GENERATED_BODY()

public:
	AMimicEcho();

protected:
	virtual void BeginPlay() override;

public:
	virtual void Tick(float DeltaTime) override;

	UFUNCTION(BlueprintCallable, Category = "Mimic|Echo")
	void SetConversationData(const TArray<FString>& Phrases, const TArray<FString>& SpeakerIDs);

	UFUNCTION(BlueprintCallable, Category = "Mimic|Echo")
	void StartPlayback();

	UFUNCTION(BlueprintCallable, Category = "Mimic|Echo")
	void StopPlayback();

	UFUNCTION(BlueprintPure, Category = "Mimic|Echo")
	bool IsPlaying() const { return bIsPlaying; }

protected:
	UPROPERTY(VisibleAnywhere, BlueprintReadOnly, Category = "Mimic|Echo")
	TObjectPtr<UAudioComponent> EchoAudioComponent;

	UPROPERTY(EditAnywhere, BlueprintReadWrite, Category = "Mimic|Echo")
	float PlaybackInterval;

	UPROPERTY(EditAnywhere, BlueprintReadWrite, Category = "Mimic|Echo")
	float TriggerRadius;

private:
	void PlayNextPhrase();
	void CheckForNearbyPlayers();

	TArray<FString> StoredPhrases;
	TArray<FString> StoredSpeakers;
	int32 CurrentPhraseIndex;
	bool bIsPlaying;
	FTimerHandle PlaybackTimer;
	FTimerHandle ProximityCheckTimer;
};
