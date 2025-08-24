using System;
using System.IO;
using FirebaseAdmin;
using Google.Apis.Auth.OAuth2;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Pipelines.Sockets.Unofficial.Arenas;

namespace Identity.Infrastructure.Firebase;

public static class FirebaseHostConfig {
    public static IServiceCollection ConfigureFirebase(
        this IServiceCollection services,
        IConfiguration configuration,
        string sectionName) {
        
        var firebaseOptions = new FirebaseOptions();
        IConfigurationSection section = configuration.GetSection(sectionName);
        section.Bind(firebaseOptions);

        if (!string.IsNullOrEmpty(firebaseOptions.AuthEmulatorHost)) {
            Environment.SetEnvironmentVariable("FIREBASE_AUTH_EMULATOR_HOST", firebaseOptions.AuthEmulatorHost);
            if (!string.IsNullOrEmpty(firebaseOptions.ProjectId))
                Environment.SetEnvironmentVariable("GOOGLE_CLOUD_PROJECT", firebaseOptions.ProjectId);

            // Admin SDK ignores the credential when using emulator,
            // but it must NOT be null. Use a throwaway access token.
            //var dummyCred = GoogleCredential.FromAccessToken("owner");
            var dummyCred = GoogleCredential.GetApplicationDefault();

            FirebaseApp.Create(new AppOptions {
                ProjectId = firebaseOptions.ProjectId,
                Credential = dummyCred
            });
        } else {
            // Production/staging: real credentials
            var cred = !string.IsNullOrEmpty(firebaseOptions.ServiceAccountJson)
                ? GoogleCredential.FromFile(firebaseOptions.ServiceAccountJson)
                : GoogleCredential.GetApplicationDefault();

            FirebaseApp.Create(new AppOptions {
                ProjectId = firebaseOptions.ProjectId,
                Credential = cred
            });
        }

        return services;
    }
}
