(function () {
    var app = angular.module('wdf.events', ['ngRoute']);

    app.controller('eventsController', ['$scope', '$rootScope', '$location', '$http', function ($scope, $rootScope, $location, $http) {
        document.addEventListener("deviceready", function () {
            $http.defaults.headers.common.Authorization = 'Bearer ' + $rootScope.user.Token;

            $scope.ready = false;
            //init page
            $scope.init = function () {

                $http.get($rootScope.fdApi + 'api/configuration/definitions/any').then(function (response) {

                    $scope.definitions = response.data;

                    $http.get($rootScope.fdApi + 'api/configuration/modifiers/any').then(function (mResponse) {

                        $scope.modifiers = mResponse.data;

                        $scope.ready = true;
                    });

                }).catch(function (error) {
                    $rootScope.handleError(error);
                    $rootScope.showError('There was an error fetching the event definitions/modifiers.');
                });

            };


            $scope.modValueConvert = function (type, v) {
                switch (type) {
                    case 0: {
                        var v2 = Math.abs(v);
                        return '+' + v2 + ' points';
                    }
                    case 1: {
                        var v2 = Math.abs(v);
                        return '-' + v2 + ' points';
                    }
                    case 2: {

                        var v2 = Math.round((v - 1) * 100);
                        var pre = v > 0 ? '+' : '';
                        return pre + v2 + '%' + ' points';
                    }
                    default: {
                        return 'invalid';
                    }
                }
            };

            $scope.init();

        });
    }]);


})();