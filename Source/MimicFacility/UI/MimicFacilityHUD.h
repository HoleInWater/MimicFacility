// MimicFacilityHUD.h — Main HUD class. Draws round info, Mimic count, and Director messages on canvas.
// Copyright (c) 2026 HoleInWater. All rights reserved.

#pragma once

#include "CoreMinimal.h"
#include "GameFramework/HUD.h"
#include "MimicFacilityHUD.generated.h"

UCLASS()
class MIMICFACILITY_API AMimicFacilityHUD : public AHUD
{
	GENERATED_BODY()

public:
	AMimicFacilityHUD();

protected:
	virtual void BeginPlay() override;

public:
	virtual void DrawHUD() override;

	UFUNCTION(BlueprintCallable, Category = "HUD")
	void ShowDirectorMessage(const FString& Message);

private:
	FString CurrentDirectorMessage;
	float DirectorMessageTimer;

	UPROPERTY(EditDefaultsOnly, Category = "HUD")
	float DirectorMessageDuration;
};
