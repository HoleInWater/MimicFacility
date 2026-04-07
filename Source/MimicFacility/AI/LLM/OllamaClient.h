// OllamaClient.h — Async HTTP client for the Ollama LLM sidecar at localhost:11434.
// Copyright (c) 2026 HoleInWater. All rights reserved.

#pragma once

#include "CoreMinimal.h"
#include "UObject/NoExportTypes.h"
#include "OllamaClient.generated.h"

DECLARE_DYNAMIC_DELEGATE_OneParam(FOnLLMResponseComplete, const FString&, Response);
DECLARE_DYNAMIC_DELEGATE_OneParam(FOnLLMResponseToken, const FString&, Token);
DECLARE_DYNAMIC_DELEGATE_OneParam(FOnLLMError, const FString&, ErrorMessage);

USTRUCT(BlueprintType)
struct FLLMRequest
{
	GENERATED_BODY()

	UPROPERTY(BlueprintReadWrite)
	FString Model;

	UPROPERTY(BlueprintReadWrite)
	FString SystemPrompt;

	UPROPERTY(BlueprintReadWrite)
	FString UserPrompt;

	UPROPERTY(BlueprintReadWrite)
	float Temperature;

	UPROPERTY(BlueprintReadWrite)
	int32 MaxTokens;

	UPROPERTY(BlueprintReadWrite)
	bool bStream;

	FLLMRequest()
		: Model(TEXT("phi3"))
		, Temperature(0.7f)
		, MaxTokens(60)
		, bStream(false)
	{}
};

USTRUCT(BlueprintType)
struct FLLMResponse
{
	GENERATED_BODY()

	UPROPERTY(BlueprintReadOnly)
	FString Text;

	UPROPERTY(BlueprintReadOnly)
	float GenerationTimeSeconds;

	UPROPERTY(BlueprintReadOnly)
	int32 TokenCount;

	UPROPERTY(BlueprintReadOnly)
	bool bSuccess;

	UPROPERTY(BlueprintReadOnly)
	FString ErrorMessage;

	FLLMResponse() : GenerationTimeSeconds(0.0f), TokenCount(0), bSuccess(false) {}
};

UCLASS(BlueprintType)
class MIMICFACILITY_API UOllamaClient : public UObject
{
	GENERATED_BODY()

public:
	UOllamaClient();

	UFUNCTION(BlueprintCallable, Category = "LLM")
	void SendRequest(const FLLMRequest& Request, FOnLLMResponseComplete OnComplete, FOnLLMError OnError);

	UFUNCTION(BlueprintCallable, Category = "LLM")
	void SetEndpoint(const FString& Host, int32 Port);

	UFUNCTION(BlueprintPure, Category = "LLM")
	bool IsAvailable() const { return bServerAvailable; }

	UFUNCTION(BlueprintCallable, Category = "LLM")
	void CheckServerHealth();

	UFUNCTION(BlueprintPure, Category = "LLM")
	FLLMResponse GetLastResponse() const { return LastResponse; }

private:
	void HandleResponse(FHttpRequestPtr Request, FHttpResponsePtr Response, bool bConnectedSuccessfully,
		FOnLLMResponseComplete OnComplete, FOnLLMError OnError, double StartTime);

	void HandleHealthCheck(FHttpRequestPtr Request, FHttpResponsePtr Response, bool bConnectedSuccessfully);

	FString BuildRequestJSON(const FLLMRequest& Request) const;
	FString ParseResponseJSON(const FString& ResponseBody) const;

	FString ServerHost;
	int32 ServerPort;
	bool bServerAvailable;
	FLLMResponse LastResponse;
};
