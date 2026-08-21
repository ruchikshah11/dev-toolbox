namespace DevToolbox.Tools.RestApiReference
{
    /// <summary>
    /// Static reference data for the "SharePoint REST API Reference" tool: the SharePoint
    /// REST/OData endpoints and query parameters used often enough to be worth a quick lookup,
    /// grouped loosely by area (web/site, lists, items, files, search/profiles, auth) followed
    /// by the OData query-string parameters that combine with almost any GET endpoint above.
    /// Not exhaustive - SharePoint's REST surface is huge - but covers the everyday operations.
    /// </summary>
    public static class RestApiReferenceData
    {
        public static readonly string[][] Rows =
        {
            // Web / Site
            new[] { "SharePoint", "Get web properties", "GET", "_api/web", "Title, Url, Description, Language, etc. of the current site" },
            new[] { "SharePoint", "Get subsites", "GET", "_api/web/webs", "Child sites directly under this web" },
            new[] { "SharePoint", "Get site collection properties", "GET", "_api/site", "Properties of the whole site collection, not just this web" },
            new[] { "SharePoint", "Get current user", "GET", "_api/web/currentuser", "The identity of the user making the call" },
            new[] { "SharePoint", "Get site users", "GET", "_api/web/siteusers", "All users known to this site" },
            new[] { "SharePoint", "Get a site user by login name", "GET", "_api/web/siteusers/getbyloginname('i:0%23.f|membership|user@domain.com')", "Login name must be URL-encoded (# becomes %23)" },
            new[] { "SharePoint", "Get site groups", "GET", "_api/web/sitegroups", "SharePoint groups defined on this site" },
            new[] { "SharePoint", "Get members of a group", "GET", "_api/web/sitegroups/getbyid(5)/users", "Replace 5 with the group's ID" },
            new[] { "SharePoint", "Get role assignments (permissions)", "GET", "_api/web/roleassignments", "Who/what has permissions directly on this web" },
            new[] { "SharePoint", "Get request digest (for writes)", "POST", "_api/contextinfo", "Response's GetContextWebInformation.FormDigestValue is the X-RequestDigest value needed for POST/MERGE/DELETE calls" },
            new[] { "SharePoint", "Get web property bag", "GET", "_api/web/allproperties", "Custom key/value properties stored on the web" },
            new[] { "SharePoint", "Set a web property bag value", "POST", "_api/web/allproperties", "Body: {\"__metadata\":{\"type\":\"SP.PropertyValues\"},\"MyKey\":\"MyValue\"}; headers: X-HTTP-Method: MERGE, IF-MATCH: *" },
            new[] { "SharePoint", "Get regional settings", "GET", "_api/web/regionalsettings", "Locale, calendar type, etc. for this web" },
            new[] { "SharePoint", "Get the web's time zone", "GET", "_api/web/regionalsettings/timezone", "The single active time zone, rather than every zone in TimeZones" },
            new[] { "SharePoint", "Get quick launch navigation", "GET", "_api/web/navigation/quicklaunch", "The left-hand nav nodes (classic sites) or Quick Launch (modern)" },
            new[] { "SharePoint", "Get top navigation bar", "GET", "_api/web/navigation/topnavigationbar", "The horizontal top nav nodes" },
            new[] { "SharePoint", "Add a navigation node", "POST", "_api/web/navigation/quicklaunch", "Body: {\"__metadata\":{\"type\":\"SP.NavigationNode\"},\"Title\":\"...\",\"Url\":\"...\",\"IsExternal\":true}; needs X-RequestDigest" },
            new[] { "SharePoint", "Get role definitions", "GET", "_api/web/roledefinitions", "Permission levels defined on this site (Full Control, Edit, Read, ...)" },
            new[] { "SharePoint", "Grant a permission on the web", "POST", "_api/web/roleassignments/addroleassignment(principalid=7,roledefid=1073741827)", "principalid is a user/group ID, roledefid a role definition ID; needs X-RequestDigest" },
            new[] { "SharePoint", "Remove a permission on the web", "POST", "_api/web/roleassignments/removeroleassignment(principalid=7,roledefid=1073741827)", "Needs X-RequestDigest" },
            new[] { "SharePoint", "Add a user to a group", "POST", "_api/web/sitegroups/getbyid(5)/users", "Body: {\"__metadata\":{\"type\":\"SP.User\"},\"LoginName\":\"i:0%23.f|membership|user@domain.com\"}; needs X-RequestDigest" },
            new[] { "SharePoint", "Remove a user from a group", "POST", "_api/web/sitegroups/getbyid(5)/users/removebyloginname(@v)?@v='i:0%23.f|membership|user@domain.com'", "Needs X-RequestDigest" },
            new[] { "SharePoint", "Get activated site features", "GET", "_api/site/features", "Site-collection-scoped features currently active" },
            new[] { "SharePoint", "Activate a feature", "POST", "_api/web/features/add(featureId=guid'00bfea71-1c5f-4a24-b310-ba51c1eb7251',force=false)", "Needs X-RequestDigest" },
            new[] { "SharePoint", "Deactivate a feature", "POST", "_api/web/features/remove(featureId=guid'00bfea71-1c5f-4a24-b310-ba51c1eb7251',force=false)", "Needs X-RequestDigest" },
            new[] { "SharePoint", "Get recycle bin items", "GET", "_api/web/recyclebin", "Items currently in this web's recycle bin" },
            new[] { "SharePoint", "Restore a recycle bin item", "POST", "_api/web/recyclebin('11111111-2222-3333-4444-555555555555')/restore()", "Needs X-RequestDigest" },
            new[] { "SharePoint", "Permanently delete a recycle bin item", "POST", "_api/web/recyclebin('11111111-2222-3333-4444-555555555555')/deleteobject()", "Needs X-RequestDigest; bypasses the recycle bin entirely" },
            new[] { "SharePoint", "Get changes since a token", "POST", "_api/web/getchanges", "Body: {\"query\":{\"__metadata\":{\"type\":\"SP.ChangeQuery\"},\"Item\":true,\"ChangeTokenStart\":{\"__metadata\":{\"type\":\"SP.ChangeToken\"},\"StringValue\":\"...\"}}}" },

            // Lists
            new[] { "SharePoint", "Get all lists", "GET", "_api/web/lists", "Every list/library on the site" },
            new[] { "SharePoint", "Get list by title", "GET", "_api/web/lists/getbytitle('ListTitle')", "Most common way to target a specific list" },
            new[] { "SharePoint", "Get list by ID", "GET", "_api/web/lists(guid'11111111-2222-3333-4444-555555555555')", "Use the list's GUID instead of its title" },
            new[] { "SharePoint", "Get a list's fields/columns", "GET", "_api/web/lists/getbytitle('ListTitle')/fields", "Includes both site columns and list-specific columns" },
            new[] { "SharePoint", "Get content types on a list", "GET", "_api/web/lists/getbytitle('ListTitle')/contenttypes", "Content types attached to this specific list" },
            new[] { "SharePoint", "Get site columns (all)", "GET", "_api/web/fields", "Every field defined at the site level" },
            new[] { "SharePoint", "Get site content types", "GET", "_api/web/contenttypes", "Every content type defined at the site level" },
            new[] { "SharePoint", "Add a site content type", "POST", "_api/web/contenttypes", "Body: {\"__metadata\":{\"type\":\"SP.ContentType\"},\"Name\":\"...\",\"ParentContentType\":{\"StringId\":\"0x0100\"}}; needs X-RequestDigest" },
            new[] { "SharePoint", "Add a content type to a list", "POST", "_api/web/lists/getbytitle('ListTitle')/contenttypes/addavailablecontenttype", "Body: {\"contentTypeId\":\"0x0101...\"}; needs X-RequestDigest" },
            new[] { "SharePoint", "Query items with CAML", "POST", "_api/web/lists/getbytitle('ListTitle')/GetItems", "Body: {\"query\":{\"__metadata\":{\"type\":\"SP.CamlQuery\"},\"ViewXml\":\"<View><Query>...</Query></View>\"}}" },
            new[] { "SharePoint", "Get a list's views", "GET", "_api/web/lists/getbytitle('ListTitle')/views", "Every view defined on the list" },
            new[] { "SharePoint", "Get a view by title", "GET", "_api/web/lists/getbytitle('ListTitle')/views/getbytitle('All Items')", "ViewQuery holds the view's CAML" },
            new[] { "SharePoint", "Get a view's fields", "GET", "_api/web/lists/getbytitle('ListTitle')/views/getbytitle('All Items')/viewfields", "The internal field names shown in this view, in order" },

            // List Items
            new[] { "SharePoint", "Get list items", "GET", "_api/web/lists/getbytitle('ListTitle')/items", "Returns up to 100 items by default; use $top and paging for more" },
            new[] { "SharePoint", "Get a single item by ID", "GET", "_api/web/lists/getbytitle('ListTitle')/items(1)", "Replace 1 with the item's ID" },
            new[] { "SharePoint", "Create an item", "POST", "_api/web/lists/getbytitle('ListTitle')/items", "Body is the item's fields as JSON with a __metadata.type; needs X-RequestDigest" },
            new[] { "SharePoint", "Update an item", "POST", "_api/web/lists/getbytitle('ListTitle')/items(1)", "Headers: X-HTTP-Method: MERGE, IF-MATCH: * (or an etag)" },
            new[] { "SharePoint", "Delete an item", "POST", "_api/web/lists/getbytitle('ListTitle')/items(1)", "Headers: X-HTTP-Method: DELETE, IF-MATCH: *" },
            new[] { "SharePoint", "Get an item's role assignments", "GET", "_api/web/lists/getbytitle('ListTitle')/items(1)/roleassignments", "Who/what has permissions on this specific item" },
            new[] { "SharePoint", "Break an item's permission inheritance", "POST", "_api/web/lists/getbytitle('ListTitle')/items(1)/breakroleinheritance(true)", "Argument controls whether existing permissions are copied first" },
            new[] { "SharePoint", "Get an item's attachments", "GET", "_api/web/lists/getbytitle('ListTitle')/items(1)/attachmentfiles", "File names and server-relative URLs of each attachment" },
            new[] { "SharePoint", "Add an attachment", "POST", "_api/web/lists/getbytitle('ListTitle')/items(1)/attachmentfiles/add(FileName='a.txt')", "Request body is the raw file bytes; needs X-RequestDigest" },
            new[] { "SharePoint", "Delete an attachment", "POST", "_api/web/lists/getbytitle('ListTitle')/items(1)/attachmentfiles/getbyfilename('a.txt')", "Headers: X-HTTP-Method: DELETE" },

            // Files / Folders
            new[] { "SharePoint", "Get folder by server-relative URL", "GET", "_api/web/getfolderbyserverrelativeurl('/sites/x/Shared Documents')", "Path is relative to the site collection root, not the web" },
            new[] { "SharePoint", "Get files in a folder", "GET", "_api/web/getfolderbyserverrelativeurl('/sites/x/Shared Documents')/files", "Does not recurse into subfolders" },
            new[] { "SharePoint", "Get subfolders", "GET", "_api/web/getfolderbyserverrelativeurl('/sites/x/Shared Documents')/folders", "Immediate child folders only" },
            new[] { "SharePoint", "Get file by server-relative URL", "GET", "_api/web/getfilebyserverrelativeurl('/sites/x/Shared Documents/a.txt')", "Returns file metadata, not the file's bytes" },
            new[] { "SharePoint", "Download file content", "GET", "_api/web/getfilebyserverrelativeurl('/sites/x/Shared Documents/a.txt')/$value", "Response body is the raw file bytes" },
            new[] { "SharePoint", "Upload a file", "POST", "_api/web/getfolderbyserverrelativeurl('/sites/x/Shared Documents')/files/add(url='name.txt',overwrite=true)", "Request body is the raw file bytes; needs X-RequestDigest" },
            new[] { "SharePoint", "Create a folder", "POST", "_api/web/folders/add('/sites/x/Shared Documents/NewFolder')", "Needs X-RequestDigest" },
            new[] { "SharePoint", "Delete a file", "POST", "_api/web/getfilebyserverrelativeurl('/sites/x/Shared Documents/a.txt')", "Headers: X-HTTP-Method: DELETE" },
            new[] { "SharePoint", "Check out a file", "POST", "_api/web/getfilebyserverrelativeurl('...')/checkout", "Needs X-RequestDigest" },
            new[] { "SharePoint", "Check in a file", "POST", "_api/web/getfilebyserverrelativeurl('...')/checkin(comment='...',checkintype=0)", "checkintype: 0=Minor, 1=Major, 2=Overwrite" },
            new[] { "SharePoint", "Get a file's versions", "GET", "_api/web/getfilebyserverrelativeurl('/sites/x/Shared Documents/a.txt')/versions", "Requires versioning to be enabled on the library" },
            new[] { "SharePoint", "Delete a file version", "POST", "_api/web/getfilebyserverrelativeurl('...')/versions/deletebyid(512)", "512 is the version's ID, not its label (e.g. \"3.0\"); needs X-RequestDigest" },

            // Search / Profiles
            new[] { "Search", "Search query", "GET", "_api/search/query?querytext='IT'", "SharePoint Search REST API (KQL-based querytext)" },
            new[] { "User Profile", "Get a user's profile properties", "GET", "_api/SP.UserProfiles.PeopleManager/GetPropertiesFor(accountName=@v)?@v='i:0%23.f|membership|user@domain.com'", "Login name must be URL-encoded and quoted" },
            new[] { "User Profile", "Get my profile properties", "GET", "_api/SP.UserProfiles.PeopleManager/GetMyProperties", "Properties for the calling user" },
            new[] { "User Profile", "Get a single profile property for a user", "GET", "_api/SP.UserProfiles.PeopleManager/GetUserProfilePropertyFor(accountName=@v,propertyName='SPS-JobTitle')?@v='i:0%23.f|membership|user@domain.com'", "One named property (e.g. SPS-JobTitle, Department) instead of the whole profile" },
            new[] { "User Profile", "Set a single-value profile property", "POST", "_api/SP.UserProfiles.PeopleManager/SetSingleValueProfileProperty", "Body: {\"accountName\":\"i:0%23.f|membership|user@domain.com\",\"propertyName\":\"SPS-JobTitle\",\"propertyValue\":\"Engineer\"}; needs X-RequestDigest; requires Manage User Profiles permission" },
            new[] { "User Profile", "Get a user's manager chain / properties by index", "GET", "_api/SP.UserProfiles.PeopleManager/GetPropertiesFor(accountName=@v)?@v='...'&$select=Manager,Email,Title", "$select narrows GetPropertiesFor the same way it does for lists/items" },
            new[] { "User Profile", "Get people I'm following", "GET", "_api/SP.UserProfiles.PeopleManager/GetPeopleFollowedByMe", "The calling user's followed-people list" },
            new[] { "User Profile", "Get who follows a user", "GET", "_api/SP.UserProfiles.PeopleManager/GetFollowersFor(accountName=@v)?@v='i:0%23.f|membership|user@domain.com'", "Accounts following the given user" },
            new[] { "User Profile", "Follow a user", "POST", "_api/SP.UserProfiles.PeopleManager/Follow(accountName=@v)?@v='i:0%23.f|membership|user@domain.com'", "Needs X-RequestDigest" },
            new[] { "User Profile", "Stop following a user", "POST", "_api/SP.UserProfiles.PeopleManager/StopFollowing(accountName=@v)?@v='i:0%23.f|membership|user@domain.com'", "Needs X-RequestDigest" },
            new[] { "User Profile", "Check if I'm following a user", "GET", "_api/SP.UserProfiles.PeopleManager/IsFollowing(possibleFolloweeAccountName=@v)?@v='i:0%23.f|membership|user@domain.com'", "Returns a plain boolean" },

            // Term Store / Taxonomy (Managed Metadata) - the newer v2.1 taxonomy API, a separate
            // REST surface from the classic _api/web endpoints above
            new[] { "Term Store", "Get the default term store", "GET", "_api/v2.1/termStore", "Top-level info: default language, languages, ID of the store itself" },
            new[] { "Term Store", "Get term groups", "GET", "_api/v2.1/termStore/groups", "Groups (e.g. \"Site Collection\", a custom department group) that organize term sets" },
            new[] { "Term Store", "Get a term group by ID", "GET", "_api/v2.1/termStore/groups/GroupId", "Replace GroupId with the group's GUID" },
            new[] { "Term Store", "Get term sets in a group", "GET", "_api/v2.1/termStore/groups/GroupId/sets", "Every term set that belongs to this group" },
            new[] { "Term Store", "Get term sets", "GET", "_api/v2.1/termStore/sets/", "Lists the term sets in the site's default term store" },
            new[] { "Term Store", "Get a term set by ID", "GET", "_api/v2.1/termStore/sets/TermSetId", "Replace TermSetId with the term set's GUID" },
            new[] { "Term Store", "Get all terms in a term set", "GET", "_api/v2.1/termStore/sets/TermSetId/terms", "Every term in the set, unfiltered" },
            new[] { "Term Store", "Get a term's labels by ID", "GET", "_api/v2.1/termStore/sets/TermSetId/terms?select=id,labels&$filter=id eq 'TermId'", "Replace TermSetId/TermId with real GUIDs; $select/$filter narrow the response to just that term's id and labels" },
            new[] { "Term Store", "Get a term's child terms", "GET", "_api/v2.1/termStore/sets/TermSetId/terms/TermId/children", "One level of children below the given term" },
            new[] { "Term Store", "Create a term in a set", "POST", "_api/v2.1/termStore/sets/TermSetId/terms", "Body: {\"labels\":[{\"name\":\"New Term\",\"isDefault\":true,\"languageTag\":\"en-US\"}]}; needs X-RequestDigest" },

            // OData query parameters (combine with almost any GET endpoint above)
            new[] { "OData", "$select", "Query Param", "?$select=Title,Id,Author/Title", "Limits returned fields; use / to reach into an expanded field" },
            new[] { "OData", "$expand", "Query Param", "?$expand=Author,AttachmentFiles", "Expands a lookup/person/attachment field so its properties are included" },
            new[] { "OData", "$filter", "Query Param", "?$filter=Title eq 'Test' and Id gt 10", "OData filter expression; string literals use single quotes" },
            new[] { "OData", "$orderby", "Query Param", "?$orderby=Modified desc", "Sorts results; append asc or desc" },
            new[] { "OData", "$top", "Query Param", "?$top=50", "Limits how many items come back (SharePoint REST caps at 5000 per request)" },
            new[] { "OData", "$skiptoken (paging)", "Query Param", "?$top=50&$skiptoken=Paged=TRUE%26p_ID=50", "Used to fetch the next page; the value comes from the previous response's __next link" },
            new[] { "OData", "Combining parameters", "Query Param", "?$select=Title,Author/Title&$expand=Author&$filter=Title ne null&$orderby=Modified desc&$top=25", "Parameters are combined with &, same as any OData query" },
        };
    }
}
