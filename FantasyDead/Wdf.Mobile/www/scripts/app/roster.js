(function () {

    var app = angular.module('wdf.roster', ['ngRoute']);

    app.controller('rosterController', ['$scope', '$rootScope', '$http', function ($scope, $rootScope, $http) {
        document.addEventListener("deviceready", function () {
            $http.defaults.headers.common.Authorization = 'Bearer ' + $rootScope.user.Token;

            $scope.showing = 'current';

            $scope.ready = false;
            $scope.init = function () {
                $scope.ready = false;

                $http.get($rootScope.fdApi + 'api/roster/bulk').then(function (response) {

                    $scope.characters = response.data.Characters;
                    $scope.show = response.data.RelatedShow;

                    var allSlots = response.data.Slots;
                    $scope.slots = [];
                    $scope.history = [];
                    $scope.episode = response.data.CurrentEpisode;

                    $.each(allSlots, function (ix, i) {

                        if (i.EpisodeId === response.data.CurrentEpisode.id)
                            $scope.slots.push(i);

                        else if (i.Occupied)
                            $scope.history.push(i);
                    });

                    $scope.ready = true;

                    setTimeout(function () {

                        var animInClass = "flipInY";
                        var animOutClass = "bounceOut";

                        $('body').on('click', '.front', function () {
                            $(this).addClass('flipOutY');
                            $('#charModal').addClass(animInClass);
                        });

                        $('#charModal').on('hide.bs.modal', function () {

                        });

                        $('#charModal').on('hidden.bs.modal', function (evt) {
                            $('#charModal').removeClass(animOutClass)
                        })
                    }, 0);

                }).catch(function (error) {
                    $rootScope.handleError(error);
                    $scope.ready = false;
                    $rootScope.showError('There was a problem fetching your roster information.');
                });

            };


            //flips a card over
            $scope.eSlot = null;
            $scope.flipSlot = function (slot) {

                if ($scope.eSlot === null) {
                    $scope.eSlot = slot;
                    $('#charModal').modal();
                } else {

                    $('#charModal').removeClass('flipInY').addClass('bounceOut');

                    $scope.eSlot = null;
                    setTimeout(function () {
                        $('#charModal').modal('hide');
                        $('#' + slot.Id + ' .front').removeClass('flipOutY').addClass('flipInY');
                    }, 500);

                }
            };

            var findSlotIx = function (id) {
                var sIx = -1;

                $.each($scope.slots, function (ix, i) {
                    if (i.Id === id) {
                        sIx = ix;
                        return false;
                    }
                });

                return sIx;
            };

            $scope.charIsSlotted = function (id) {

                var results = $.grep($scope.slots, function (e, ix) {
                    if (!e.Pick)
                        return false;

                    return (e.Pick.CharacterId === id);
                });

                return results.length > 0;
            };

            $scope.selectChar = function (ch) {
                if (ch === null) {
                    //unslot
                    return;
                }

                if ($scope.charIsSlotted(ch))
                    return;

                var slotType = $scope.eSlot.DeathSlot ? 1 : 0;
                //api/roster/pick/show/{showId}/character/{characterId}/slot/{slotType}
                $http.put($rootScope.fdApi + 'api/roster/pick/show/' + $scope.show.id + '/character/' + ch + '/slot/' + slotType, null)
                .then(function (response) {
                    var slotIx = findSlotIx($scope.eSlot.Id);
                    $scope.slots[slotIx] = response.data;
                    $scope.flipSlot($scope.slots[slotIx]);
                }).catch(function (error) {
                    $scope.flipSlot($scope.eSlot);
                    $rootScope.handleError(error);
                    $rootScope.showError(error);
                });
            };

            $scope.init();
        });
    }]);


})();