// SubliminalFrameSystem.h — Flashes subliminal text on the HUD for exactly one frame.
// Copyright (c) 2026 HoleInWater. All rights reserved.

#pragma once

#include "CoreMinimal.h"
#include "UObject/NoExportTypes.h"
#include "SubliminalFrameSystem.generated.h"

class UCanvas;

UCLASS(BlueprintType)
class MIMICFACILITY_API USubliminalFrameSystem : public UObject
{
	GENERATED_BODY()

public:
	USubliminalFrameSystem();

	UFUNCTION(BlueprintCallable, Category = "Horror|Subliminal")
	void RenderSubliminalFrame();

	bool ConsumeFrame(FString& OutMessage) const;

private:
	UPROPERTY()
	TArray<FString> Messages;

	mutable bool bFramePending;
	mutable FString PendingMessage;
};
