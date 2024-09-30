﻿namespace wumgr;

internal static class UpdateErrors
{
    public static string GetErrorStr(uint id)
    {
        switch (id)
        {
            case 0x00240001: return "Windows Update Agent was stopped successfully.";
            case 0x00240002: return "Windows Update Agent updated itself.";
            case 0x00240003: return "Operation completed successfully but there were errors applying the updates.";
            case 0x00240004:
                return
                    "A callback was marked to be disconnected later because the request to disconnect the operation came while a callback was executing.";
            case 0x00240005: return "The system must be restarted to complete installation of the update.";
            case 0x00240006: return "The update to be installed is already installed on the system.";
            case 0x00240007: return "The update to be removed is not installed on the system.";
            case 0x00240008: return "The update to be downloaded has already been downloaded.";
            case 0x00240009:
                return
                    "The operation completed successfully, but some updates were skipped because the system is running on batteries.";
            case 0x00242015: return "The installation operation for the update is still in progress.";
            case 0x00245001: return "No operation was required by the server-initiated healing server response.";
            case 0x00246001: return "The update to be downloaded is already being downloaded.";
            case 0x80240001: return "Windows Update Agent was unable to provide the service.";
            case 0x80240002: return "The maximum capacity of the service was exceeded.";
            case 0x80240003: return "An ID cannot be found.";
            case 0x80240004: return "The object could not be initialized.";
            case 0x80240005:
                return "The update handler requested a byte range overlapping a previously requested range.";
            case 0x80240006: return "The requested number of byte ranges exceeds the maximum number (2^31 - 1).";
            case 0x80240007: return "The index to a collection was invalid.";
            case 0x80240008: return "The key for the item queried could not be found.";
            case 0x80240009:
                return
                    "Another conflicting operation was in progress. Some operations such as installation cannot be performed twice simultaneously.";
            case 0x8024000A: return "Cancellation of the operation was not allowed.";
            case 0x8024000B: return "Operation was cancelled.";
            case 0x8024000C: return "No operation was required.";
            case 0x8024000D:
                return "Windows Update Agent could not find required information in the update's XML data.";
            case 0x8024000E: return "Windows Update Agent found invalid information in the update's XML data.";
            case 0x8024000F: return "Circular update relationships were detected in the metadata.";
            case 0x80240010: return "Update relationships too deep to evaluate were evaluated.";
            case 0x80240011: return "An invalid update relationship was detected.";
            case 0x80240012: return "An invalid registry value was read.";
            case 0x80240013: return "Operation tried to add a duplicate item to a list.";
            case 0x80240014: return "Updates requested for install are not installable by caller.";
            case 0x80240016:
                return
                    "Operation tried to install while another installation was in progress or the system was pending a mandatory restart.";
            case 0x80240017: return "Operation was not performed because there are no applicable updates.";
            case 0x80240018: return "Operation failed because a required user token is missing.";
            case 0x80240019: return "An exclusive update cannot be installed with other updates at the same time.";
            case 0x8024001A: return "A policy value was not set.";
            case 0x8024001B:
                return "The operation could not be performed because the Windows Update Agent is self-updating.";
            case 0x8024001D: return "An update contains invalid metadata.";
            case 0x8024001E: return "Operation did not complete because the service or system was being shut down.";
            case 0x8024001F: return "Operation did not complete because the network connection was unavailable.";
            case 0x80240020: return "Operation did not complete because there is no logged-on interactive user.";
            case 0x80240021: return "Operation did not complete because it timed out.";
            case 0x80240022: return "Operation failed for all the updates.";
            case 0x80240023: return "The license terms for all updates were declined.";
            case 0x80240024: return "There are no updates.";
            case 0x80240025: return "Group Policy settings prevented access to Windows Update.";
            case 0x80240026: return "The type of update is invalid.";
            case 0x80240027: return "The URL exceeded the maximum length.";
            case 0x80240028:
                return "The update could not be uninstalled because the request did not originate from a WSUS server.";
            case 0x80240029:
                return "Search may have missed some updates before there is an unlicensed application on the system.";
            case 0x8024002A: return "A component required to detect applicable updates was missing.";
            case 0x8024002B: return "An operation did not complete because it requires a newer version of server.";
            case 0x8024002C: return "A delta-compressed update could not be installed because it required the source.";
            case 0x8024002D: return "A full-file update could not be installed because it required the source.";
            case 0x8024002E: return "Access to an unmanaged server is not allowed.";
            case 0x8024002F: return "Operation did not complete because the DisableWindowsUpdateAccess policy was set.";
            case 0x80240030: return "The format of the proxy list was invalid.";
            case 0x80240031: return "The file is in the wrong format.";
            case 0x80240032: return "The search criteria string was invalid.";
            case 0x80240033: return "License terms could not be downloaded.";
            case 0x80240034: return "Update failed to download.";
            case 0x80240035: return "The update was not processed.";
            case 0x80240036: return "The object's current state did not allow the operation.";
            case 0x80240037: return "The functionality for the operation is not supported.";
            case 0x80240038: return "The downloaded file has an unexpected content type.";
            case 0x80240039: return "Agent is asked by server to resync too many times.";
            case 0x80240040: return "WUA API method does not run on Server Core installation.";
            case 0x80240041: return "Service is not available while sysprep is running.";
            case 0x80240042: return "The update service is no longer registered with AU.";
            case 0x80240043: return "There is no support for WUA UI.";
            case 0x80240044: return "Only administrators can perform this operation on per-machine updates.";
            case 0x80240045:
                return "A search was attempted with a scope that is not currently supported for this type of search.";
            case 0x80240046: return "The URL does not point to a file.";
            case 0x80240047: return "The operation requested is not supported.";
            case 0x80240048: return "The featured update notification info returned by the server is invalid.";
            case 0x80240049: return "The data is out of range.";
            case 0x8024004A: return "Windows Update agent operations are not available while OS setup is running.";
            case 0x8024004B: return "An orphaned downloadjob was found with no active callers.";
            case 0x8024004C:
                return "An update could not be installed because the system battery power level is too low.";
            case 0x8024004D: return "The downloaded infrastructure file is incorrectly formatted.";
            case 0x8024004E: return "The infrastructure file must be downloaded using strong SSL.";
            case 0x8024004F:
                return "A discovery call contributed to a non-zero operation count at idle timer shutdown.";
            case 0x80240050: return "A search call contributed to a non-zero operation count at idle timer shutdown.";
            case 0x80240051: return "A download call contributed to a non-zero operation count at idle timer shutdown.";
            case 0x80240052: return "An install call contributed to a non-zero operation count at idle timer shutdown.";
            case 0x80240053:
                return "An unspecified call contributed to a non-zero operation count at idle timer shutdown.";
            case 0x80240054:
                return
                    "An interactive user cancelled this operation, which was started from the Windows Update Agent UI.";
            case 0x80240055:
                return
                    "Automatic Updates cancelled this operation because it applies to an update that is no longer applicable to this computer.";
            case 0x80240056:
                return "This version or edition of the operating system doesn't support the needed functionality.";
            case 0x80240057:
                return
                    "The requested update download or install handler, or update applicability expression evaluator, is not provided by this Agent plugin.";
            case 0x80240058: return "The requested serialization version is not supported.";
            case 0x80240059:
                return "The current network cost does not meet the conditions set by the network cost policy.";
            case 0x8024005A:
                return
                    "The call is cancelled because it applies to an update that is hidden (no longer applicable to this computer).";
            case 0x8024005B:
                return
                    "The call is cancelled because it applies to an update that is invalid (no longer applicable to this computer).";
            case 0x8024005C: return "The specified volume id is invalid.";
            case 0x8024005D: return "The specified volume id is unrecognized by the system.";
            case 0x8024005E: return "The installation extended error code is not specified.";
            case 0x8024005F: return "The installation extended error code is set to general fail.";
            case 0x80240FFF: return "An operation failed due to reasons not covered by another error code.";
            case 0x80241001:
                return "Search may have missed some updates because the Windows Installer is less than version 3.1.";
            case 0x80241002:
                return "Search may have missed some updates because the Windows Installer is not configured.";
            case 0x80241003:
                return "Search may have missed some updates because policy has disabled Windows Installer patching.";
            case 0x80241004: return "An update could not be applied because the application is installed per-user.";
            case 0x80241005:
                return "Search may have missed some updates because the Windows Installer is less than version 3.1.";
            case 0x80241FFF:
                return "Search may have missed some updates because there was a failure of the Windows Installer.";
            case 0x80244000:
                return "WU_E_PT_SOAPCLIENT_* error codes map to the SOAPCLIENT_ERROR enum of the ATL Server Library.";
            case 0x80244001:
                return
                    "Same as SOAPCLIENT_INITIALIZE_ERROR - initialization of the SOAP client failed, possibly because of an MSXML installation failure.";
            case 0x80244002: return "Same as SOAPCLIENT_OUTOFMEMORY - SOAP client failed because it ran out of memory.";
            case 0x80244003: return "Same as SOAPCLIENT_GENERATE_ERROR - SOAP client failed to generate the request.";
            case 0x80244004: return "Same as SOAPCLIENT_CONNECT_ERROR - SOAP client failed to connect to the server.";
            case 0x80244005:
                return
                    "Same as SOAPCLIENT_SEND_ERROR - SOAP client failed to send a message for reasons of WU_E_WINHTTP_* error codes.";
            case 0x80244006:
                return "Same as SOAPCLIENT_SERVER_ERROR - SOAP client failed because there was a server error.";
            case 0x80244007:
                return
                    "Same as SOAPCLIENT_SOAPFAULT - SOAP client failed because there was a SOAP fault for reasons of WU_E_PT_SOAP_* error codes.";
            case 0x80244008: return "Same as SOAPCLIENT_PARSEFAULT_ERROR - SOAP client failed to parse a SOAP fault.";
            case 0x80244009:
                return "Same as SOAPCLIENT_READ_ERROR - SOAP client failed while reading the response from the server.";
            case 0x8024400A:
                return "Same as SOAPCLIENT_PARSE_ERROR - SOAP client failed to parse the response from the server.";
            case 0x8024400B:
                return
                    "Same as SOAP_E_VERSION_MISMATCH - SOAP client found an unrecognizable namespace for the SOAP envelope.";
            case 0x8024400C: return "Same as SOAP_E_MUST_UNDERSTAND - SOAP client was unable to understand a header.";
            case 0x8024400D:
                return "Same as SOAP_E_CLIENT - SOAP client found the message was malformed; fix before resending.";
            case 0x8024400E:
                return
                    "Same as SOAP_E_SERVER - The SOAP message could not be processed due to a server error; resend later.";
            case 0x8024400F: return "There was an unspecified Windows Management Instrumentation (WMI) error.";
            case 0x80244010: return "The number of round trips to the server exceeded the maximum limit.";
            case 0x80244011: return "WUServer policy value is missing in the registry.";
            case 0x80244012: return "Initialization failed because the object was already initialized.";
            case 0x80244013: return "The computer name could not be determined.";
            case 0x80244015:
                return
                    "The reply from the server indicates that the server was changed or the cookie was invalid; refresh the state of the internal cache and retry.";
            case 0x80244016:
                return "Same as HTTP status 400 - the server could not process the request due to invalid syntax.";
            case 0x80244017: return "Same as HTTP status 401 - the requested resource requires user authentication.";
            case 0x80244018:
                return "Same as HTTP status 403 - server understood the request, but declined to fulfill it.";
            case 0x80244019:
                return
                    "Same as HTTP status 404 - the server cannot find the requested URI (Uniform Resource Identifier).";
            case 0x8024401A: return "Same as HTTP status 405 - the HTTP method is not allowed.";
            case 0x8024401B: return "Same as HTTP status 407 - proxy authentication is required.";
            case 0x8024401C: return "Same as HTTP status 408 - the server timed out waiting for the request.";
            case 0x8024401D:
                return
                    "Same as HTTP status 409 - the request was not completed due to a conflict with the current state of the resource.";
            case 0x8024401E:
                return "Same as HTTP status 410 - requested resource is no longer available at the server.";
            case 0x8024401F:
                return "Same as HTTP status 500 - an error internal to the server prevented fulfilling the request.";
            case 0x80244020:
                return
                    "Same as HTTP status 500 - server does not support the functionality required to fulfill the request.";
            case 0x80244021:
                return
                    "Same as HTTP status 502 - the server, while acting as a gateway or proxy, received an invalid response from the upstream server it accessed in attempting to fulfill the request.";
            case 0x80244022: return "Same as HTTP status 503 - the service is temporarily overloaded.";
            case 0x80244023: return "Same as HTTP status 503 - the request was timed out waiting for a gateway.";
            case 0x80244024:
                return
                    "Same as HTTP status 505 - the server does not support the HTTP protocol version used for the request.";
            case 0x80244025:
                return "Operation failed due to a changed file location; refresh internal state and resend.";
            case 0x80244026:
                return
                    "Operation failed because Windows Update Agent does not support registration with a non-WSUS server.";
            case 0x80244027: return "The server returned an empty authentication information list.";
            case 0x80244028: return "Windows Update Agent was unable to create any valid authentication cookies.";
            case 0x80244029: return "A configuration property value was wrong.";
            case 0x8024402A: return "A configuration property value was missing.";
            case 0x8024402B:
                return
                    "The HTTP request could not be completed and the reason did not correspond to any of the WU_E_PT_HTTP_* error codes.";
            case 0x8024402C:
                return
                    "Same as ERROR_WINHTTP_NAME_NOT_RESOLVED - the proxy server or target server name cannot be resolved.";
            case 0x8024502D:
                return
                    "Windows Update Agent failed to download a redirector cabinet file with a new redirectorId value from the server during the recovery.";
            case 0x8024502E: return "A redirector recovery action did not complete because the server is managed.";
            case 0x8024402F: return "External cab file processing completed with some errors.";
            case 0x80244030: return "The external cab processor initialization did not complete.";
            case 0x80244031: return "The format of a metadata file was invalid.";
            case 0x80244032: return "External cab processor found invalid metadata.";
            case 0x80244033: return "The file digest could not be extracted from an external cab file.";
            case 0x80244034: return "An external cab file could not be decompressed.";
            case 0x80244035: return "External cab processor was unable to get file locations.";
            case 0x80240436:
                return
                    "The server does not support category-specific search; Full catalog search has to be issued instead.";
            case 0x80240437: return "There was a problem authorizing with the service.";
            case 0x80240438: return "There is no route or network connectivity to the endpoint.";
            case 0x80240439: return "The data received does not meet the data contract expectations.";
            case 0x8024043A: return "The url is invalid.";
            case 0x8024043B: return "Unable to load NWS runtime.";
            case 0x8024043C: return "The proxy auth scheme is not supported.";
            case 0x8024043D: return "The requested service property is not available.";
            case 0x8024043E: return "The endpoint provider plugin requires online refresh.";
            case 0x8024043F: return "A URL for the requested service endpoint is not available.";
            case 0x80240440: return "The connection to the service endpoint died.";
            case 0x80240441: return "The operation is invalid because protocol talker is in an inappropriate state.";
            case 0x80240442: return "The object is in a faulted state due to a previous error.";
            case 0x80240443: return "The operation would lead to numeric overflow.";
            case 0x80240444: return "The operation was aborted.";
            case 0x80240445: return "The operation was abandoned.";
            case 0x80240446: return "A quota was exceeded.";
            case 0x80240447: return "The information was not available in the specified language.";
            case 0x80240448: return "The address is already being used.";
            case 0x80240449: return "The address is not valid for this context.";
            case 0x8024044A: return "Unrecognized error occurred in the Windows Web Services framework.";
            case 0x8024044B: return "A security operation failed in the Windows Web Services framework.";
            case 0x80244FFF: return "A communication error not covered by another WU_E_PT_* error code.";
            case 0x80245001: return "The redirector XML document could not be loaded into the DOM class.";
            case 0x80245002: return "The redirector XML document is missing some required information.";
            case 0x80245003: return "The redirectorId in the downloaded redirector cab is less than in the cached cab.";
            case 0x80245004: return "The service ID is not supported in the service environment.";
            case 0x80245005: return "The response from the redirector server had an unsupported content type.";
            case 0x80245006: return "The response from the redirector server had an error status or was invalid.";
            case 0x80245008: return "The maximum number of name value pairs was exceeded by the attribute provider.";
            case 0x80245009: return "The name received from the attribute provider was invalid.";
            case 0x8024500A: return "The value received from the attribute provider was invalid.";
            case 0x8024500B:
                return
                    "There was an error in connecting to or parsing the response from the Service Locator Service redirector server.";
            case 0x8024500C: return "Connections to the redirector server are disallowed by managed policy.";
            case 0x8024500D: return "The redirector would go online but is disallowed by caller configuration.";
            case 0x802450FF:
                return "The redirector failed for reasons not covered by another WU_E_REDIRECTOR_* error code.";
            case 0x80245101: return "Verification of the servicing engine package failed.";
            case 0x80245102: return "Verification of a servicing package failed.";
            case 0x80245103: return "Verification of the staged engine failed.";
            case 0x80245104: return "Verification of a staged payload failed.";
            case 0x80245105: return "An internal error occurred where the servicing action was not found.";
            case 0x80245106: return "There was a parse error in the service environment response.";
            case 0x80245107: return "A downloaded file failed an integrity check.";
            case 0x80245108: return "No engine was provided by the server-initiated healing server response.";
            case 0x802451FF: return "There was a failure for reasons not covered by another WU_E_SIH_* error code.";
            case 0x8024C001: return "A driver was skipped.";
            case 0x8024C002:
                return "A property for the driver could not be found. It may not conform with required specifications.";
            case 0x8024C003: return "The registry type read for the driver does not match the expected type.";
            case 0x8024C004: return "The driver update is missing metadata.";
            case 0x8024C005: return "The driver update is missing a required attribute.";
            case 0x8024C006: return "Driver synchronization failed.";
            case 0x8024C007: return "Information required for the synchronization of applicable printers is missing.";
            case 0x8024C008: return "After installing a driver update, the updated device has reported a problem.";
            case 0x8024CFFF: return "A driver error not covered by another WU_E_DRV_* code.";
            case 0x80248000: return "An operation failed because Windows Update Agent is shutting down.";
            case 0x80248001: return "An operation failed because the data store was in use.";
            case 0x80248002: return "The current and expected states of the data store do not match.";
            case 0x80248003: return "The data store is missing a table.";
            case 0x80248004: return "The data store contains a table with unexpected columns.";
            case 0x80248005: return "A table could not be opened because the table is not in the data store.";
            case 0x80248006: return "The current and expected versions of the data store do not match.";
            case 0x80248007: return "The information requested is not in the data store.";
            case 0x80248008:
                return
                    "The data store is missing required information or has a NULL in a table column that requires a non-null value.";
            case 0x80248009:
                return
                    "The data store is missing required information or has a reference to missing license terms, file, localized property or linked row.";
            case 0x8024800A: return "The update was not processed because its update handler could not be recognized.";
            case 0x8024800B:
                return "The update was not deleted because it is still referenced by one or more services.";
            case 0x8024800C: return "The data store section could not be locked within the allotted time.";
            case 0x8024800D:
                return
                    "The category was not added because it contains no parent categories and is not a top-level category itself.";
            case 0x8024800E: return "The row was not added because an existing row has the same primary key.";
            case 0x8024800F: return "The data store could not be initialized because it was locked by another process.";
            case 0x80248010: return "The data store is not allowed to be registered with COM in the current process.";
            case 0x80248011: return "Could not create a data store object in another process.";
            case 0x80248013: return "The server sent the same update to the client with two different revision IDs.";
            case 0x80248014: return "An operation did not complete because the service is not in the data store.";
            case 0x80248015:
                return "An operation did not complete because the registration of the service has expired.";
            case 0x80248016:
                return
                    "A request to hide an update was declined because it is a mandatory update or because it was deployed with a deadline.";
            case 0x80248017: return "A table was not closed because it is not associated with the session.";
            case 0x80248018: return "A table was not closed because it is not associated with the session.";
            case 0x80248019:
                return
                    "A request to remove the Windows Update service or to unregister it with Automatic Updates was declined because it is a built-in service and/or Automatic Updates cannot fall back to another service.";
            case 0x8024801A: return "A request was declined because the operation is not allowed.";
            case 0x8024801B:
                return
                    "The schema of the current data store and the schema of a table in a backup XML document do not match.";
            case 0x8024801C:
                return "The data store requires a session reset; release the session and retry with a new session.";
            case 0x8024801D:
                return
                    "A data store operation did not complete because it was requested with an impersonated identity.";
            case 0x80248FFF: return "A data store error not covered by another WU_E_DS_* code.";
            case 0x80249001: return "Parsing of the rule file failed.";
            case 0x80249002: return "Failed to get the requested inventory type from the server.";
            case 0x80249003: return "Failed to upload inventory result to the server.";
            case 0x80249004: return "There was an inventory error not covered by another error code.";
            case 0x80249005: return "A WMI error occurred when enumerating the instances for a particular class.";
            case 0x8024A000: return "Automatic Updates was unable to service incoming requests.";
            case 0x8024A002:
                return
                    "The old version of the Automatic Updates client has stopped because the WSUS server has been upgraded.";
            case 0x8024A003: return "The old version of the Automatic Updates client was disabled.";
            case 0x8024A004: return "Automatic Updates was unable to process incoming requests because it was paused.";
            case 0x8024A005: return "No unmanaged service is registered with AU.";
            case 0x8024A006: return "The default service registered with AU changed during the search.";
            case 0x8024A007: return "A reboot is in progress.";
            case 0x8024A008:
                return "Automatic Updates can't process incoming requests while Windows Welcome is running.";
            case 0x8024AFFF: return "An Automatic Updates error not covered by another WU_E_AU * code.";
            case 0x80242000:
                return
                    "A request for a remote update handler could not be completed because no remote process is available.";
            case 0x80242001:
                return
                    "A request for a remote update handler could not be completed because the handler is local only.";
            case 0x80242002:
                return
                    "A request for an update handler could not be completed because the handler could not be recognized.";
            case 0x80242003: return "A remote update handler could not be created because one already exists.";
            case 0x80242004:
                return
                    "A request for the handler to install (uninstall) an update could not be completed because the update does not support install (uninstall).";
            case 0x80242005: return "An operation did not complete because the wrong handler was specified.";
            case 0x80242006:
                return "A handler operation could not be completed because the update contains invalid metadata.";
            case 0x80242007:
                return "An operation could not be completed because the installer exceeded the time limit.";
            case 0x80242008: return "An operation being done by the update handler was cancelled.";
            case 0x80242009:
                return "An operation could not be completed because the handler-specific metadata is invalid.";
            case 0x8024200A:
                return
                    "A request to the handler to install an update could not be completed because the update requires user input.";
            case 0x8024200B: return "The installer failed to install (uninstall) one or more updates.";
            case 0x8024200C:
                return
                    "The update handler should download self-contained content rather than delta-compressed content for the update.";
            case 0x8024200D:
                return "The update handler did not install the update because it needs to be downloaded again.";
            case 0x8024200E:
                return
                    "The update handler failed to send notification of the status of the install (uninstall) operation.";
            case 0x8024200F:
                return "The file names contained in the update metadata and in the update package are inconsistent.";
            case 0x80242010: return "The update handler failed to fall back to the self-contained content.";
            case 0x80242011: return "The update handler has exceeded the maximum number of download requests.";
            case 0x80242012: return "The update handler has received an unexpected response from CBS.";
            case 0x80242013: return "The update metadata contains an invalid CBS package identifier.";
            case 0x80242014: return "The post-reboot operation for the update is still in progress.";
            case 0x80242015: return "The result of the post-reboot operation for the update could not be determined.";
            case 0x80242016:
                return "The state of the update after its post-reboot operation has completed is unexpected.";
            case 0x80242017:
                return "The OS servicing stack must be updated before this update is downloaded or installed.";
            case 0x80242018: return "A callback installer called back with an error.";
            case 0x80242019:
                return "The custom installer signature did not match the signature required by the update.";
            case 0x8024201A: return "The installer does not support the installation configuration.";
            case 0x8024201B: return "The targeted session for install is invalid.";
            case 0x8024201C: return "The handler failed to decrypt the update files.";
            case 0x8024201D: return "The update handler is disabled until the system reboots.";
            case 0x8024201E: return "The AppX infrastructure is not present on the system.";
            case 0x8024201F:
                return "The update cannot be committed because it has not been previously installed or staged.";
            case 0x80242020: return "The specified volume is not a valid AppX package volume.";
            case 0x80242021: return "The configured default storage volume is unavailable.";
            case 0x80242022: return "The volume on which the application is installed is unavailable.";
            case 0x80242023: return "The specified package family is not present on the system.";
            case 0x80242024: return "Unable to find a package volume marked as system.";
            case 0x80242FFF: return "An update handler error not covered by another WU_E_UH_* code.";
            case 0x80246001:
                return
                    "A download manager operation could not be completed because the requested file does not have a URL.";
            case 0x80246002:
                return
                    "A download manager operation could not be completed because the file digest was not recognized.";
            case 0x80246003:
                return
                    "A download manager operation could not be completed because the file metadata requested an unrecognized hash algorithm.";
            case 0x80246004:
                return
                    "An operation could not be completed because a download request is required from the download handler.";
            case 0x80246005:
                return
                    "A download manager operation could not be completed because the network connection was unavailable.";
            case 0x80246006:
                return
                    "A download manager operation could not be completed because the version of Background Intelligent Transfer Service (BITS) is incompatible.";
            case 0x80246007: return "The update has not been downloaded.";
            case 0x80246008:
                return
                    "A download manager operation failed because the download manager was unable to connect the Background Intelligent Transfer Service (BITS).";
            case 0x80246009:
                return
                    "A download manager operation failed because there was an unspecified Background Intelligent Transfer Service (BITS) transfer error.";
            case 0x8024600A:
                return "A download must be restarted because the location of the source of the download has changed.";
            case 0x8024600B:
                return "A download must be restarted because the update content changed in a new revision.";
            case 0x8024600C:
                return
                    "A download failed because the current network limits downloads by update size for the update service.";
            case 0x8024600E:
                return "The download failed because the client was denied authorization to download the content.";
            case 0x8024600F:
                return "The download failed because the user token associated with the BITS job no longer exists.";
            case 0x80246010: return "The sandbox directory for the downloaded update was not found.";
            case 0x80246011: return "The downloaded update has an unknown file path.";
            case 0x80246012: return "One or more of the files for the downloaded update is missing.";
            case 0x80246013: return "An attempt was made to access a downloaded update that has already been removed.";
            case 0x80246014: return "Windows Update couldn't find a needed portion of a downloaded update's file.";
            case 0x80246016:
                return
                    "The download failed because the client was denied authorization to download the content due to no user logged on.";
            case 0x80246017:
                return "The download failed because the local user was denied authorization to download the content.";
            case 0x80246018:
                return "The download failed because the domain user was denied authorization to download the content.";
            case 0x80246019:
                return
                    "The download failed because the MSA account associated with the user was denied authorization to download the content.";
            case 0x8024601A: return "The download will be continued by falling back to BITS to download the content.";
            case 0x8024601B: return "Another caller has requested download to a different volume.";
            case 0x8024601C: return "The hash of the update's sandbox does not match the expected value.";
            case 0x80246FFF: return "There was a download manager error not covered by another WU_E_DM_* error code.";
            case 0x8024D001:
                return "Windows Update Agent could not be updated because an INF file contains invalid information.";
            case 0x8024D002:
                return
                    "Windows Update Agent could not be updated because the wuident.cab file contains invalid information.";
            case 0x8024D003:
                return
                    "Windows Update Agent could not be updated because of an internal error that caused setup initialization to be performed twice.";
            case 0x8024D004:
                return
                    "Windows Update Agent could not be updated because setup initialization never completed successfully.";
            case 0x8024D005:
                return
                    "Windows Update Agent could not be updated because the versions specified in the INF do not match the actual source file versions.";
            case 0x8024D006:
                return
                    "Windows Update Agent could not be updated because a WUA file on the target system is newer than the corresponding source file.";
            case 0x8024D007: return "Windows Update Agent could not be updated because regsvr32.exe returned an error.";
            case 0x8024D008:
                return
                    "An update to the Windows Update Agent was skipped because previous attempts to update have failed.";
            case 0x8024D009:
                return "An update to the Windows Update Agent was skipped due to a directive in the wuident.cab file.";
            case 0x8024D00A:
                return
                    "Windows Update Agent could not be updated because the current system configuration is not supported.";
            case 0x8024D00B:
                return
                    "Windows Update Agent could not be updated because the system is configured to block the update.";
            case 0x8024D00C:
                return "Windows Update Agent could not be updated because a restart of the system is required.";
            case 0x8024D00D: return "Windows Update Agent setup is already running.";
            case 0x8024D00E: return "Windows Update Agent setup package requires a reboot to complete installation.";
            case 0x8024D00F:
                return "Windows Update Agent could not be updated because the setup handler failed during execution.";
            case 0x8024D010:
                return "Windows Update Agent could not be updated because the registry contains invalid information.";
            case 0x8024D011: return "Windows Update Agent must be updated before search can continue.";
            case 0x8024D012:
                return
                    "Windows Update Agent must be updated before search can continue. An administrator is required to perform the operation.";
            case 0x8024D013:
                return
                    "Windows Update Agent could not be updated because the server does not contain update information for this version.";
            case 0x8024D014:
                return "Windows Update Agent is successfully updated, but a reboot is required to complete the setup.";
            case 0x8024D015:
                return "Windows Update Agent is successfully updated, but a reboot is required to complete the setup.";
            case 0x8024D016: return "Windows Update Agent could not be updated because of an unknown error.";
            case 0x8024DFFF:
                return
                    "Windows Update Agent could not be updated because of an error not covered by another WU_E_SETUP_* error code.";
            case 0x8024E001:
                return
                    "An expression evaluator operation could not be completed because an expression was unrecognized.";
            case 0x8024E002:
                return "An expression evaluator operation could not be completed because an expression was invalid.";
            case 0x8024E003:
                return
                    "An expression evaluator operation could not be completed because an expression contains an incorrect number of metadata nodes.";
            case 0x8024E004:
                return
                    "An expression evaluator operation could not be completed because the version of the serialized expression data is invalid.";
            case 0x8024E005: return "The expression evaluator could not be initialized.";
            case 0x8024E006:
                return
                    "An expression evaluator operation could not be completed because there was an invalid attribute.";
            case 0x8024E007:
                return
                    "An expression evaluator operation could not be completed because the cluster state of the computer could not be determined.";
            case 0x8024EFFF:
                return "There was an expression evaluator error not covered by another WU_E_EE_* error code.";
            case 0x80243001:
                return
                    "The results of download and installation could not be read from the registry due to an unrecognized data format version.";
            case 0x80243002:
                return
                    "The results of download and installation could not be read from the registry due to an invalid data format.";
            case 0x80243003:
                return
                    "The results of download and installation are not available; the operation may have failed to start.";
            case 0x80243004:
                return "A failure occurred when trying to create an icon in the taskbar notification area.";
            case 0x80243FFD: return "Unable to show UI when in non-UI mode; WU client UI modules may not be installed.";
            case 0x80243FFE: return "Unsupported version of WU client UI exported functions.";
            case 0x80243FFF:
                return "There was a user interface error not covered by another WU_E_AUCLIENT_* error code.";
            case 0x8024F001: return "The event cache file was defective.";
            case 0x8024F002: return "The XML in the event namespace descriptor could not be parsed.";
            case 0x8024F003: return "The XML in the event namespace descriptor could not be parsed.";
            case 0x8024F004: return "The server rejected an event because the server was too busy.";
            case 0x8024F005: return "The specified callback cookie is not found.";
            case 0x8024FFFF: return "There was a reporter error not covered by another error code.";
            case 0x80247001: return "An operation could not be completed because the scan package was invalid.";
            case 0x80247002:
                return
                    "An operation could not be completed because the scan package requires a greater version of the Windows Update Agent.";
            case 0x80247003: return "An invalid event payload was specified.";
            case 0x80247004: return "The size of the event payload submitted is invalid.";
            case 0x80247005: return "The service is not registered.";
            case 0x80247FFF: return "Search using the scan package failed.";
            case 0x8024B001: return "The task is currently in progress.";
            case 0x8024B002: return "The operation cannot be completed since the task status is currently disabled.";
            case 0x8024B003: return "The operation cannot be completed since the task is not yet started.";
            case 0x8024B004: return "The task was stopped and needs to be run again to complete.";
            case 0x8024B005: return "Cannot cancel a non-scheduled install.";
            case 0x8024B101: return "Hardware capability meta data was not found after a sync with the service.";
            case 0x8024B102: return "Hardware capability meta data was malformed and/or failed to parse.";
            default: return "Unknown Error: " + id;
        }
    }
}
