// MimicFacilityGameMode.h — Primary game mode. Wires up all systems and manages player registration.
// Copyright (c) 2026 HoleInWater. All rights reserved.

#pragma once

#include "CoreMinimal.h"
#include "GameFramework/GameModeBase.h"
#include "MimicFacilityGameMode.generated.h"

class ARoundManager;
class ADirectorAI;

UCLASS()
class MIMICFACILITY_API AMimicFacilityGameMode : public AGameModeBase
{
	GENERATED_BODY()

public:
	AMimicFacilityGameMode();

	UFUNCTION(BlueprintCallable, Category = "GameMode")
	ARoundManager* GetRoundManager() const { return RoundManagerInstance; }

	UFUNCTION(BlueprintCallable, Category = "GameMode")
	ADirectorAI* GetDirectorAI() const { return DirectorInstance; }

	virtual void PostLogin(APlayerController* NewPlayer) override;

protected:
	virtual void BeginPlay() override;
	virtual void InitGame(const FString& MapName, const FString& Options, FString& ErrorMessage) override;

private:
	UPROPERTY()
	TObjectPtr<ARoundManager> RoundManagerInstance;

	UPROPERTY()
	TObjectPtr<ADirectorAI> DirectorInstance;

	int32 NextSubjectNumber;
};
