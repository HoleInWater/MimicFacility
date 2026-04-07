// SporeVent.h — Spore emitter controlled by the Director. Damages overlapping characters.
// Copyright (c) 2026 HoleInWater. All rights reserved.

#pragma once

#include "CoreMinimal.h"
#include "GameFramework/Actor.h"
#include "SporeVent.generated.h"

class USphereComponent;
class UNiagaraComponent;

UCLASS()
class MIMICFACILITY_API ASporeVent : public AActor
{
	GENERATED_BODY()

public:
	ASporeVent();

	UFUNCTION(BlueprintCallable, Category = "Facility|Spore")
	void Activate();

	UFUNCTION(BlueprintCallable, Category = "Facility|Spore")
	void Deactivate();

	UFUNCTION(BlueprintPure, Category = "Facility|Spore")
	bool IsActive() const { return bIsActive; }

	UPROPERTY(EditAnywhere, BlueprintReadWrite, Category = "Facility")
	FName ZoneTag;

	UPROPERTY(EditAnywhere, BlueprintReadWrite, Category = "Facility|Spore")
	float SporeRadius;

	UPROPERTY(EditAnywhere, BlueprintReadWrite, Category = "Facility|Spore")
	float SporeDamageRate;

protected:
	virtual void BeginPlay() override;
	virtual void GetLifetimeReplicatedProps(TArray<FLifetimeProperty>& OutLifetimeProps) const override;

	UPROPERTY(VisibleAnywhere, BlueprintReadOnly, Category = "Components")
	TObjectPtr<USphereComponent> EffectRadius;

	UPROPERTY(VisibleAnywhere, BlueprintReadOnly, Category = "Components")
	TObjectPtr<UNiagaraComponent> SporeVFX;

	UPROPERTY(ReplicatedUsing = OnRep_IsActive, BlueprintReadOnly, Category = "Facility|Spore")
	bool bIsActive;

private:
	UFUNCTION()
	void OnRep_IsActive();

	UFUNCTION()
	void OnOverlapBegin(UPrimitiveComponent* OverlappedComp, AActor* OtherActor, UPrimitiveComponent* OtherComp, int32 OtherBodyIndex, bool bFromSweep, const FHitResult& SweepResult);

	void ApplySporeDamage();

	FTimerHandle DamageTimerHandle;
};
