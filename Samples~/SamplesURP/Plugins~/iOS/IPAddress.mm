#import <ifaddrs.h>
#import <arpa/inet.h>

extern "C" const char* _GetWiFiIPAddress()
{
    struct ifaddrs *interfaces = NULL;
    struct ifaddrs *temp_addr = NULL;
    static char addressBuffer[INET_ADDRSTRLEN];

    getifaddrs(&interfaces);
    temp_addr = interfaces;

    while (temp_addr != NULL) {
        if (temp_addr->ifa_addr->sa_family == AF_INET) {
            if (strcmp(temp_addr->ifa_name, "en0") == 0) { // WiFi
                getnameinfo(temp_addr->ifa_addr, sizeof(struct sockaddr_in), addressBuffer, INET_ADDRSTRLEN, NULL, 0, NI_NUMERICHOST);
                break;
            }
        }
        temp_addr = temp_addr->ifa_next;
    }

    freeifaddrs(interfaces);
    return addressBuffer;
}
