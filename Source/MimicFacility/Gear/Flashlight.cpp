// Flashlight.cpp — Flashlight implementation.
// Copyright (c) 2026 HoleInWater. All rights reserved.

#include "Flashlight.h"
#include "Components/SpotLightComponent.h"
#include "Net/UnrealNetwork.h"

AFlashlight::AFlashlight()
{
	bIsConsumable = false;
	bIsOn = false;

	SpotLight = CreateDefaultSubobject<USpotLightComponent>(TEXT("SpotLight"));
	SpotLight->SetupAttachment(RootComponent);
	SpotLight->SetVisibility(false);
}

void AFlashlight::Activate()
{
	bIsOn = !bIsOn;
	SpotLight->SetVisibility(bIsOn);
}
