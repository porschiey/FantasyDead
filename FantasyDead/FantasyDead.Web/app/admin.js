(function () {

    var app = angular.module('admin', ['ngRoute', 'ui.bootstrap.datetimepicker']);

    app.config(['$routeProvider', function ($routeProvider) {


        $routeProvider
            .when('/landing',
            {
                templateUrl: '/app/landing.html',
                controller: 'landingController'
            })
            .when('/login',
            {
                templateUrl: '/app/login.html',
                controller: 'loginController'
            })
              .when('/characters',
            {
                templateUrl: '/app/characters.html',
                controller: 'charactersController'
            })
              .when('/events',
            {
                templateUrl: '/app/events.html',
                controller: 'eventsController'
            })
              .when('/score',
            {
                templateUrl: '/app/score.html',
                controller: 'scoreController'
            })
        .otherwise('/landing');



    }]);


    app.run(['$rootScope', '$location', '$http', function ($rootScope, $location, $http) {


        $rootScope.user = null;
        var authObj = getCookie('fdAuth');

        if (authObj !== '') {
            try {
                var user = JSON.parse(atob(authObj));
                $rootScope.user = user;
                $http.defaults.headers.common.Authorization = 'Bearer ' + getCookie('fdToken');
                $rootScope.loggedIn = true;
            } catch (e) {
                //swallow & clear 
                $rootScope.signOut(false);
                console.warn('Could not log user in from cookie.');
            }
        }

        $rootScope.$on('$locationChangeStart', function (ev, newUrl) {
            if ($rootScope.user === null && newUrl.indexOf('login') == -1) {
                $rootScope.loggedIn = false;
                $location.path('/login');
            }
        });

        $rootScope.errorMsg = null;

        //handles any text error.
        $rootScope.handleError = function (msg) {
            $rootScope.errorMsg = msg;
        };

        //handles any response error.
        $rootScope.handleHttpError = function (httpResponse) {
            if (httpResponse.data === '') {
                httpResponse.data = { Message: httpResponse.statusText };
            }
            $rootScope.errorMsg = httpResponse.status + ': ' + httpResponse.data.Message;
        }

        //closes error dialog box
        $rootScope.closeError = function () {
            $rootScope.errorMsg = null;
        };

        //signs the user out
        $rootScope.signOut = function (redirect) {
            if (redirect === undefined)
                redirect = true;
            $rootScope.loggedIn = false;
            $rootScope.user = null;
            setCookie('fdAuth', '', -1);
            setCookie('fdToken', '', -1);
            $location.path('/login');
        };

        //$rootScope.readDate = function (dateStr) {
        //    return new Date(dateStr);
        //}
    }]);


    app.controller('charactersController', ['$scope', '$rootScope', '$http', function ($scope, $rootScope, $http) {

    }]);



    app.controller('eventsController', ['$scope', '$rootScope', '$http', function ($scope, $rootScope, $http) {

    }]);


    app.controller('landingController', ['$scope', '$rootScope', '$http', function ($scope, $rootScope, $http) {

        $scope.init = function () {
            $http.get('api/configuration/shows').then(function (response) {
                $scope.shows = response.data;
                $scope.ready = true;
            }).catch($rootScope.handleHttpError);
        };

        $scope.ready = false;
        $scope.init();


        $scope.editEpisode = function (ep, showId, seasonId) {

            if (!ep)
                ep = {
                    ShowId: showId,
                    SeasonId: seasonId
                };

            $scope.eEp = ep;
            $('#editEp').modal();
        };

        $scope.saving = false;
        $scope.saveEpisode = function () {

            $scope.saving = true;
            $http.put('api/show/season/episode', $scope.eEp).then(function (response) {
                $('#editEp').modal('hide');
                $scope.init();
            }).catch(function (error) {
                $scope.saving = false;
                $('#editEp').modal('hide');
                $rootScope.handleHttpError(error);
            });
        };
    }]);


    app.controller('scoreController', ['$scope', '$rootScope', '$http', function ($scope, $rootScope, $http) {

    }]);

    ///////////// LOGIN
    app.controller('loginController', ['$scope', '$rootScope', '$http', '$location', function ($scope, $rootScope, $http, $location) {

        $scope.id = {};
        $scope.working = false;
        $scope.signIn = function (id) {
            $scope.working = true;
            if (id.Credentials === '' || id.PlatformUserId === '') {
                $rootScope.handleError('Email / Password cannot be blank.');
                $scope.working = false;
                return;
            }

            id.Credentials = btoa(id.Credentials);
            id.PlatformName = 'Custom';

            $http.put('/api/register/login', id).then(function (response) {

                var token = response.data;
                $http.defaults.headers.common.Authorization = 'Bearer ' + token;

                $http.get('api/person').then(function (pResponse) {

                    $rootScope.user = pResponse.data;

                    var uJsonData = btoa(JSON.stringify(pResponse.data));
                    setCookie('fdAuth', uJsonData, 7);
                    setCookie('fdToken', token, 7);
                    $rootScope.loggedIn = true;
                    $location.path('/landing');
                });

            }).catch(function (response) {
                $rootScope.handleHttpError(response);
                $scope.id.Credentials = '';
                $scope.working = false;
            });

        };

    }]);

    var setCookie = function (cname, cvalue, exdays) {
        var d = new Date();
        d.setTime(d.getTime() + (exdays * 24 * 60 * 60 * 1000));
        var expires = "expires=" + d.toUTCString();
        document.cookie = cname + "=" + cvalue + ";" + expires + ";path=/";
    };

    var getCookie = function (cname) {
        var name = cname + "=";
        var ca = document.cookie.split(';');
        for (var i = 0; i < ca.length; i++) {
            var c = ca[i];
            while (c.charAt(0) == ' ') {
                c = c.substring(1);
            }
            if (c.indexOf(name) == 0) {
                return c.substring(name.length, c.length);
            }
        }
        return "";
    };

})();