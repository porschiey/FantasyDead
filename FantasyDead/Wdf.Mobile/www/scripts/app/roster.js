(function () {

    var app = angular.module('wdf.roster', ['ngRoute']);

    app.controller('rosterController', ['$scope', '$rootScope', function ($scope, $rootScope) {
        document.addEventListener("deviceready", function () {


            $scope.showing = 'current';
        });
    }]);


})();