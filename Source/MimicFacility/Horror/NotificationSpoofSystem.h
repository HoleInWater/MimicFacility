// NotificationSpoofSystem.h — Plays OS-matching notification sounds to blur game/reality boundary.
// Copyright (c) 2026 HoleInWater. All rights reserved.

#pragma once

#include "CoreMinimal.h"
#include "UObject/NoExportTypes.h"
#include "NotificationSpoofSystem.generated.h"

class USoundBase;
class UAudioComponent;

UCLASS(BlueprintType)
class MIMICFACILITY_API UNotificationSpoofSystem : public UObject
{
	GENERATED_BODY()

public:
	UNotificationSpoofSystem();

	UFUNCTION(BlueprintCallable, Category = "Horror|Notification")
	void PlayNotificationSound();

private:
	USoundBase* ResolveNotificationSound() const;

	UPROPERTY()
	TSoftObjectPtr<USoundBase> WindowsNotificationSound;

	UPROPERTY()
	TSoftObjectPtr<USoundBase> MacNotificationSound;

	UPROPERTY()
	TSoftObjectPtr<USoundBase> LinuxNotificationSound;
};
