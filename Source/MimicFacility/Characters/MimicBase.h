// MimicBase.h — Base class for all Mimic enemy types. Handles skin duplication, voice playback, and behavior tree binding.
// Copyright (c) 2026 HoleInWater. All rights reserved.

#pragma once

#include "CoreMinimal.h"
#include "GameFramework/Character.h"
#include "MimicBase.generated.h"

class UAudioComponent;

UENUM(BlueprintType)
enum class EMimicState : uint8
{
	Infiltrating    UMETA(DisplayName = "Infiltrating"),
	Stalking        UMETA(DisplayName = "Stalking"),
	Aggressive      UMETA(DisplayName = "Aggressive"),
	Reproducing     UMETA(DisplayName = "Reproducing")
};

UCLASS()
class MIMICFACILITY_API AMimicBase : public ACharacter
{
	GENERATED_BODY()

public:
	AMimicBase();

protected:
	virtual void BeginPlay() override;

public:
	virtual void Tick(float DeltaTime) override;

	UFUNCTION(BlueprintCallable, Category = "Mimic")
	void SetMimicState(EMimicState NewState);

	UFUNCTION(BlueprintCallable, Category = "Mimic")
	EMimicState GetMimicState() const { return CurrentState; }

	UFUNCTION(BlueprintCallable, Category = "Mimic")
	void SetVoiceProfile(const FString& PlayerID);

	UFUNCTION(BlueprintCallable, Category = "Mimic")
	void MarkIdentified();

	UFUNCTION(BlueprintPure, Category = "Mimic")
	bool IsIdentified() const { return bIsIdentified; }

protected:
	UPROPERTY(ReplicatedUsing = OnRep_MimicSkin, BlueprintReadOnly, Category = "Mimic")
	FString VoiceProfileID;

	UPROPERTY(Replicated, BlueprintReadOnly, Category = "Mimic")
	EMimicState CurrentState;

	UPROPERTY(Replicated, BlueprintReadOnly, Category = "Mimic")
	bool bIsIdentified;

	UPROPERTY(VisibleAnywhere, BlueprintReadOnly, Category = "Mimic")
	TObjectPtr<UAudioComponent> VoicePlaybackComponent;

	UFUNCTION()
	void OnRep_MimicSkin();

	virtual void GetLifetimeReplicatedProps(TArray<FLifetimeProperty>& OutLifetimeProps) const override;
};
