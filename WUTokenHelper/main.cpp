#include <winrt/Windows.Foundation.h>
#include <winrt/Windows.Foundation.Collections.h>
#include <winrt/Windows.Security.Credentials.h>
#include <winrt/Windows.Security.Authentication.Web.Core.h>
#include <winrt/Windows.Security.Cryptography.h>
#include "winrt/Windows.Internal.Security.Authentication.Web.h"

#include <combaseapi.h>

#include <thread>

using namespace winrt;
using namespace Windows::Foundation;
using namespace Windows::Security::Authentication::Web::Core;
using namespace Windows::Internal::Security::Authentication::Web;
using namespace Windows::Security::Credentials;
using namespace Windows::Security::Cryptography;

#define WU_NO_ACCOUNT MAKE_HRESULT(SEVERITY_ERROR, FACILITY_ITF, 0x200)

void TryGetToken(hstring scope, winrt::Windows::Security::Credentials::WebAccount& accountInfo, winrt::hstring& tokenBase64)
{
	auto accountProvider = WebAuthenticationCoreManager::FindAccountProviderAsync(L"https://login.microsoft.com", L"consumers").get();
	WebTokenRequest request(accountProvider, scope, L"{268761a2-03f3-40df-8a8b-c3db24145b6b}");
	auto result = WebAuthenticationCoreManager::GetTokenSilentlyAsync(request, accountInfo).get();
	auto token = result.ResponseData().GetAt(0).Token();
	wprintf(L"Token = %s\n", token.c_str());
	auto tokenBinary = CryptographicBuffer::ConvertStringToBinary(token, BinaryStringEncoding::Utf16LE);
	tokenBase64 = CryptographicBuffer::EncodeToBase64String(tokenBinary);
	wprintf(L"Encoded token = %s\n", tokenBase64.c_str());
}

extern "C" __declspec(dllexport) int  __stdcall GetWUToken(char* scope, wchar_t** retToken) {
	auto tokenBrokerStatics = get_activation_factory<TokenBrokerInternal, Windows::Foundation::IUnknown>();
	auto statics = tokenBrokerStatics.as<ITokenBrokerInternalStatics>();
	auto accounts = statics.FindAllAccountsAsync().get();
	wprintf(L"Account count = %i\n", accounts.Size());
	if (accounts.Size() == 0)
		return WU_NO_ACCOUNT;

	hstring tokenBase64;

	// Loop the accounts on the system incase
	// the first doesn't have access to the token
	for (size_t i = 0; i < accounts.Size(); i++)
	{
		auto accountInfo = accounts.GetAt(i);
		wprintf(L"ID = %s\n", accountInfo.Id().c_str());
		wprintf(L"Name = %s\n", accountInfo.UserName().c_str());

		try {
			TryGetToken(to_hstring(scope), accountInfo, tokenBase64);
		}
		catch (...) {
			if (i == accounts.Size() - 1) {
				throw std::current_exception();
			}
		}

		if (tokenBase64.size() >= 1) {
			break;
		}
	}

	*retToken = (wchar_t*)::CoTaskMemAlloc((tokenBase64.size() + 1) * sizeof(wchar_t));
	memcpy(*retToken, tokenBase64.data(), (tokenBase64.size() + 1) * sizeof(wchar_t));

	return S_OK;
}
