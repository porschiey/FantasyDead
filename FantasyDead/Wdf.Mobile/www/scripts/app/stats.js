﻿(function () {
    var app = angular.module('wdf.stats', ['ngRoute']);

    app.controller('statsController', ['$scope', '$rootScope', '$location', '$http', function ($scope, $rootScope, $location, $http) {
        document.addEventListener("deviceready", function () {
            $http.defaults.headers.common.Authorization = 'Bearer ' + $rootScope.user.Token;

            //init page
            $scope.init = function () {

            };


            $scope.init();

        });
    }]);


})();