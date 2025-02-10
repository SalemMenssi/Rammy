var LocalStoragePlugin = {
  GetLocalStorageToken: function () {
    var token = localStorage.getItem("unity_token") || "";
    console.log({"something" : token}) ; 
    return token.length > 0 ? token : "";
  },
};

mergeInto(LibraryManager.library, LocalStoragePlugin);
